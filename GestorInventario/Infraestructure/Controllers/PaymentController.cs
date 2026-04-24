using GestorInventario.Application.DTOs.Rembolso;
using GestorInventario.Infraestructure.Repositories;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.Paypal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;


namespace GestorInventario.Infraestructure.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IPaypalOrderService _paypalOrderService;  
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IPaymentRepository _paymentRepository; 
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IPaypalRefundService _paypalRefundService;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IPedidoManagementService _pedidoService;
        public PaymentController(ILogger<PaymentController> logger,   ICurrentUserAccessor current, IPedidoRepository pedido,
            IPolicyExecutor executor, IPaypalOrderService service, IPaypalRefundService pay, IPaymentRepository payment,
            IPedidoManagementService pedidoService)
        {
            _logger = logger;
           
            _policyExecutor = executor;
            _paypalOrderService = service;
            _paymentRepository = payment;          
            _currentUserAccessor = current;
            _paypalRefundService = pay;
            _pedidoRepository = pedido;
            _pedidoService = pedidoService;
           
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


                return RedirectToAction("DetallesPagoEjecutado", "Pedidos", new { id = orderId });
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
            await _paymentRepository.LimpiarPedidoCorruptoUsuarioAsync(usuarioActual);
            return RedirectToAction("Index", "Productos");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefundSale([FromBody] RefundRequestModelDto request)
        {
            if (request == null || request.PedidoId <= 0)
                return BadRequest("Datos inválidos");

            try
            {
                await _paypalRefundService.RefundSaleAsync(request.PedidoId, request.Currency);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefundPartial([FromBody] RefundRequestModelDto request)
        {
            if (request?.PedidoId <= 0)
            {
                return Json(new { success = false, message = "Solicitud inválida." });
            }

            try
            {
                await _paypalRefundService.RefundPartialAsync(request.PedidoId, request.Currency,request.Motivo);
               
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [Authorize]
        public IActionResult FormularioRembolso()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FormularioRembolso(RefundFormViewModel form)
        {
            try
            {
                var obtenerNumeroPedido= await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.ObtenerNumeroPedido(form));

                if (obtenerNumeroPedido == null)
                {
                    _logger.LogInformation("El numero de pedido proporcionado no existe " + obtenerNumeroPedido);
                }

                int usuarioActual = _currentUserAccessor.GetCurrentUserId();

                var emailCliente =  _policyExecutor.ExecutePolicy(() => _currentUserAccessor.GetCurrentUserEmail());
                if(emailCliente == null)
                {
                    _logger.LogInformation("El email proporcionado no se encuentra registrado "+ emailCliente);
                }
              

                var pedido = await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.ObtenerNumeroPedido(form));

                if (pedido == null || pedido.Data== null)
                {
                    
                   _logger.LogInformation("El pedido con el numero de pedido proporcionado no existe ");
                    return View(nameof(FormularioRembolso));
                }
               
                var detallespago = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paypalOrderService.ObtenerDetallesPagoEjecutadoV2(pedido.Data.OrderId));

                if (detallespago == null)
                {
                    _logger.LogInformation("No se ha podido obtener los detalles del pago");
                    return View(nameof(FormularioRembolso));
                }

                // Verificar que hay purchase units
                if (detallespago.PurchaseUnits == null || !detallespago.PurchaseUnits.Any())
                {
                    _logger.LogInformation("No se encuntran las unidades de pago en la peticion");
                }

                var firstPurchaseUnit = detallespago.PurchaseUnits.First();

                var detallesSuscripcion = _paymentRepository.ProcesarDetallesSuscripcion(detallespago);

                // Lista para almacenar los ítems de PayPal
                var paypalItems = await _paymentRepository.ProcesarRembolso(firstPurchaseUnit, detallesSuscripcion.Data,usuarioActual,form,obtenerNumeroPedido.Data,emailCliente);
                if (User.IsAdministrador())
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "Pedido");
                }

               
            }
            catch (Exception ex)
            {
                // Loggear el error
                _logger.LogError(ex, "Error al procesar el reembolso");
                return StatusCode(500, "Ocurrió un error al procesar tu solicitud");
            }
        }
       
    }      
}