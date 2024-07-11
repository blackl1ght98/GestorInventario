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

namespace GestorInventario.Infraestructure.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        public PaymentController(ILogger<PaymentController> logger, IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
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
                // Obtén el contexto de la API de PayPal
                var apiContext = new APIContext(new OAuthTokenCredential(_configuration["Paypal:ClientId"], _configuration["Paypal:ClientSecret"]).GetAccessToken());

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
                string returnUrl = "https://localhost:7056/Payment/Success";
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
