using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



namespace GestorInventario.Infraestructure.Controllers.PaypalControllers
{
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IPaypalOrderService _paypalOrderService;  
        private readonly IPolicyExecutor _policyExecutor;     
        private readonly ICurrentUserAccessor _currentUserAccessor;     
        private readonly IPedidoManagementService _pedidoService;
        private readonly IPaymentService _paymentService;
        public PaymentController(
            ILogger<PaymentController> logger,   
            ICurrentUserAccessor currentUser,       
            IPolicyExecutor policyExecutor, 
            IPaypalOrderService paypalOrderService,     
            IPedidoManagementService pedidoService, 
            IPaymentService paymentService)
        {
            _logger = logger;           
            _policyExecutor = policyExecutor;
            _paypalOrderService = paypalOrderService;             
            _currentUserAccessor = currentUser;  
            _pedidoService = pedidoService;
            _paymentService = paymentService;
           
        }
        [Authorize]
        public async Task<IActionResult> Success(string token=null)
        {
            try
            {
               //Capturar el id de la url de paypal
                var orderId = token ?? string.Empty;
                //CaptureId->representa el id del pago en paypal
                //total-> lo que has pagado
                //currency-> la moneda
                var (captureId, total, currency) = await _paypalOrderService.CapturarPagoAsync(orderId);
                

                var usuarioActual = _currentUserAccessor.GetCurrentUserId();
 
                var pedido =  await _pedidoService.ConfirmarPagoDelPedidoAsync(usuarioActual,captureId,total,currency,orderId);


                return RedirectToAction("DetallesPagoEjecutado", "Pagos", new { id = orderId, pedidoId= pedido.Data.Id });
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder, inténtelo de nuevo más tarde";
                _logger.LogError(ex, "Error al realizar el pago");
                return RedirectToAction("Error", "Home");
            }
        }
        [Authorize]
        public async Task<IActionResult> Cancel()
        {
            var usuarioActual = _currentUserAccessor.GetCurrentUserId();
            await _paymentService.LimpiarPedidoCorruptoUsuarioAsync(usuarioActual);
            return RedirectToAction("Index", "Productos");
        }
       
       
    }      
}