using GestorInventario.Domain.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using PayPal.Api;
using System.Globalization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GestorInventario.Domain.Models.ViewModels.Paypal;

namespace GestorInventario.Infraestructure.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;
        public PaymentController(ILogger<PaymentController> logger, IUnitOfWork unitOfWork, IConfiguration configuration, GestorInventarioContext context)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
        [AllowAnonymous]
        public async Task<IActionResult> Success(string paymentId, string PayerID)
        {
            try
            {
                var clientId = _configuration["Paypal:ClientId"] ?? Environment.GetEnvironmentVariable("Paypal_ClientId");
                var clientSeecret = _configuration["Paypal:ClientSecret"] ?? Environment.GetEnvironmentVariable("Paypal_ClientSecret");
                // Obtén el contexto de la API de PayPal
                var apiContext = new APIContext(new OAuthTokenCredential(clientId, clientSeecret).GetAccessToken());
          
                // Crea un objeto PaymentExecution para ejecutar la transacción
                var paymentExecution = new PaymentExecution() { payer_id = PayerID };

                // Obtén el pago que se va a ejecutar
                var paymentToExecute = new Payment() { id = paymentId };

                // Ejecuta el pago
                var executedPayment = paymentToExecute.Execute(apiContext, paymentExecution);

                if (executedPayment.state.ToLower() != "approved")
                {

                    TempData["ErrorMessage"] = "Error en el pago del pedido";
                }
                var pagoId = executedPayment.id;
                var saleId = executedPayment.transactions[0].related_resources[0].sale.id;              
                var total = executedPayment.transactions[0].related_resources[0].sale.amount.total;
                var currency = executedPayment.transactions[0].related_resources[0].sale.amount.currency;

               

                var existeUsuario = User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    // Aquí puedes almacenar el saleId en tu base de datos asociado al pedido
                    var pedido = await _context.Pedidos
                    .Where(p => p.IdUsuario == usuarioId && p.EstadoPedido == "En Proceso")
                    .OrderByDescending(p => p.FechaPedido)
                    .FirstOrDefaultAsync();

                    if (pedido != null)
                    {
                        pedido.SaleId = saleId;
                        pedido.Total = total;
                        pedido.Currency = currency;
                        pedido.PagoId = pagoId;
                        pedido.EstadoPedido = "Pagado";
                        _context.Update(pedido);
                        await _context.SaveChangesAsync();
                    }
                }
                   
                // Aquí puedes almacenar el saleId en tu base de datos asociado al pedido
                // Ejemplo: GuardarSaleIdEnBaseDeDatos(pedidoId, saleId);
                return View();
            }
            catch (Exception ex)
            {

                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al realizar el pago");
                return RedirectToAction("Error", "Home");
            }
           
        }
    
        [HttpPost]
        public async Task<IActionResult> RefundSale([FromBody] RefundRequestModel request)
        {
            if (request == null || request.PedidoId <= 0)
            {
                return BadRequest("Solicitud inválida.");
            }

            try
            {
                var refund = await _unitOfWork.PaypalService.RefundSaleAsync(request.PedidoId,  request.currency);
                return Ok(refund);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al realizar el reembolso: {ex.Message}");
            }
        }
      

        [HttpPost]
        public async Task<IActionResult> PayUsingCard(PaymentViewModel model)
        {
            // Cambia la cultura actual del hilo a InvariantCulture
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            // Cambia la cultura de la interfaz de usuario actual del hilo a InvariantCulture
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

            try
            {
                if (model.amount == 0)
                {
                    TempData["error"] = "Por favor introduzca lo que va a donar";
                    return RedirectToAction(nameof(Index));
                }

                string amountString = model.amount.ToString().Replace(",", ".");
                decimal amount = Decimal.Parse(amountString, CultureInfo.InvariantCulture);
                var isDocker = Environment.GetEnvironmentVariable("IS_DOCKER") == "true";
                string returnUrl = isDocker
                    ? _configuration["Paypal:returnUrlConDocker"] ?? Environment.GetEnvironmentVariable("Paypal_returnUrlConDocker")
                    : _configuration["Paypal:returnUrlSinDocker"] ?? Environment.GetEnvironmentVariable("Paypal_returnUrlSinDocker");

                string cancelUrl = "https://localhost:7056/Payment/Cancel";
                var createdPayment = await _unitOfWork.PaypalService.CreateDonation(amount, returnUrl, cancelUrl, model.currency);
                string approvalUrl = createdPayment.links.FirstOrDefault(x => x.rel.ToLower() == "approval_url")?.href;
                if (!string.IsNullOrEmpty(approvalUrl))
                {
                    return Redirect(approvalUrl);
                }
                else
                {
                    TempData["error"] = "Fallo al iniciar el pago con paypal";
                }
            }
            catch (Exception ex)
            {

                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al realizar el pago");
                return RedirectToAction("Error", "Home");

            }
            return RedirectToAction(nameof(Index));
        }
    }
}
