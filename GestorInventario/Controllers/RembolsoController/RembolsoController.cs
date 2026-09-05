using GestorInventario.Application.Services.Common;
using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Domain.Models;
using GestorInventario.Extensions;
using GestorInventario.Interfaces.Application.MetodosPaginacion;
using GestorInventario.Interfaces.Application.RetryPolicy;
using GestorInventario.Interfaces.Application.Services.BackgroundServices;
using GestorInventario.Interfaces.Application.Services.Orders;
using GestorInventario.Interfaces.Application.Services.Payment;
using GestorInventario.Interfaces.Application.Services.Paypal.Mapping;
using GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Refunds;
using GestorInventario.Interfaces.Application.Services.Refunds;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Notifications.SendNotification.Email;
using GestorInventario.Interfaces.Web;
using GestorInventario.Shared.DTOS.Paypal.BD;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Order;
using GestorInventario.Shared.DTOS.Rembolso;
using GestorInventario.Shared.Utilities;
using GestorInventario.ViewModels.Paypal;
using GestorInventario.ViewModels.Refunds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Globalization;

namespace GestorInventario.Controllers.RembolsoController
{
   
    public class RembolsoController : Controller
    {
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IRembolsoRepository _rembolsoRepository;       
        private readonly ILogger<RembolsoController> _logger;
        private readonly IPaginationHelper _paginationHelper;     
        private readonly IPedidoRepository _pedidoRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IPaypalOrderService _paypalOrderService;
        private readonly IPaymentService _paymentService;
        private readonly IPayPalOrderMappingService _mappingService;   
        private readonly IRefundService _refundService;
        private readonly IBackgroundTaskQueue _background;
      
        private readonly IPaypalRefundService _paypalRefundService;
        public RembolsoController(
            IPolicyExecutor policyExecutor, 
            IRembolsoRepository rembolsoRepository, 
             ILogger<RembolsoController> logger, 
             IPaginationHelper paginationHelper,      
             IPedidoRepository pedidoRepository,
             ICurrentUserAccessor currentUserAccessor,
             IPaypalOrderService paypalOrderService,
             IPaymentService paymentService,
             IPayPalOrderMappingService mappingService,
             IRefundService refundService,
             IBackgroundTaskQueue provider,   
             IPaypalRefundService refund
            )
        {
            _policyExecutor = policyExecutor;
            _rembolsoRepository = rembolsoRepository;  
            _logger = logger;
            _paginationHelper = paginationHelper;
            _pedidoRepository = pedidoRepository;
            _currentUserAccessor = currentUserAccessor;
            _paypalOrderService = paypalOrderService;
            _paymentService = paymentService;
            _mappingService = mappingService;      
            _refundService = refundService;
            _background = provider;
            _paypalRefundService = refund;


        }
        [Authorize(Policy = "EsAdministrador")]
        public async Task<IActionResult> Index(string buscar, [FromQuery] Paginacion paginacion)
        {
            try
            {


                var queryable = await _policyExecutor.ExecutePolicyAsync(() => _rembolsoRepository.ObtenerRembolsos());
                if (!string.IsNullOrEmpty(buscar))
                {
                    queryable = queryable.Where(s => s.NumeroPedido.Contains(buscar));
                }
                // 🔹 Usamos el helper directamente
                var paginationResult = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paginationHelper.PaginarAsync(queryable, paginacion)
                );

                var viewModel = new RefundsViewModel
                {
                    Rembolsos = paginationResult.Items, 
                    Paginas = paginationResult.Paginas.ToList(),
                    TotalPaginas = paginationResult.TotalPaginas,
                    PaginaActual = paginacion.Pagina,
                    Buscar = buscar
                };   
                return View(viewModel);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al obtener los datos del usuario");
                return RedirectToAction("Error", "Home");
            }
        }
        [HttpDelete("{id}")]   
        [Authorize(Policy = "EsAdministrador")]
        public async Task<IActionResult> EliminarRembolso(int id)
        {
            var success = await _policyExecutor.ExecutePolicyAsync(() => _rembolsoRepository.EliminarRembolso(id));

            if (success.Success)
            {
                return Json(new { success = true });
            }
            else
            {
                TempData["ErrorMessage"] = success.Message;
                return Json(new { success = false, errorMessage = success.Message });
            }
        }
        [HttpPost]
        [Authorize(Policy = "EsAdministrador")]
        public async Task<IActionResult> RefundSale([FromBody] RefundFullDto request)
        {
            if (request == null || request.PedidoId <= 0)
                return BadRequest("Datos inválidos");

            try
            {
                
                var pedido = await _pedidoRepository
                    .ObtenerPedidoConDetallesAsync(request.PedidoId);
                
                if (pedido == null)
                    return NotFound("Pedido no encontrado");
               
                var reembolsoPrevio = pedido.Rembolsos?
                    .FirstOrDefault(r => r.ReembolsoCompletado == true);

                if (reembolsoPrevio != null)
                {
                    _logger.LogWarning(
                        "Reembolso duplicado rechazado - pedido {PedidoId} refundIdPrevio={RefundId}",
                        pedido.Id, reembolsoPrevio.NumeroPedido);

                    return Ok(new
                    {
                        success = true,
                        alreadyProcessed = true,
                        refundId = reembolsoPrevio.NumeroPedido
                    });
                }
                var captureId = pedido.PayPalPaymentCaptures.FirstOrDefault().CaptureId;
                if (string.IsNullOrEmpty(captureId))
                    return BadRequest("El pedido no tiene pago capturado para reembolsar");

                var totalReembolso = pedido.Total;
                if (totalReembolso <= 0)
                    return BadRequest("El total del pedido no es válido para reembolso");

                _logger.LogInformation(
                    "Reembolso total pedido {PedidoId} -> Subtotal:{Subtotal} IVA:{Iva} Total:{Total}",
                    request.PedidoId, pedido.Subtotal, pedido.Iva, totalReembolso);

               
                    
                    var refundResult = await _paypalRefundService.RefundCaptureAsync(
                        captureId: captureId,
                        amount: totalReembolso,
                        currency: request.Currency,
                        nota: $"Reembolso pedido #{pedido.NumeroPedido}");

                    if (!refundResult.Success)
                        return BadRequest(new { success = false, message = refundResult.Message });

                   
                  var procesar =  await _refundService.ProcesarRembolsoAsync(
                        pedido.Id,
                        EstadoPedido.Rembolsado.ToString(),
                        refundResult.Data.RefundId);
                if (procesar.Success)
                {
                  
                    _background.Enqueue(async (sp, ct) =>
                    {
                        var notificar = sp.GetRequiredService<IRefundNotification>();
                        await notificar.EnviarEmailNotificacionRembolso(
                            pedido.Id,
                            refundResult.Data.AmountRefunded,
                            "Reembolso Aprobado");
                    });
                    return Ok(new { success = true, refundId = refundResult.Data.RefundId });
                }
                else
                {
                    return BadRequest(new { success = false, message = procesar.Message });
                }
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en refund pedido {PedidoId}", request.PedidoId);
                return StatusCode(500, new { success = false, message = "Error procesando reembolso" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "EsAdministrador")]
        public async Task<IActionResult> RefundPartial([FromBody] RefundPartialDto request)
        {
            if (request?.DetalleId <= 0)
            {
                return Json(new { success = false, message = "Solicitud inválida." });
            }

            try
            {

                var resultado = await _refundService.RealizarRembolsoParcial(request);
                var total = CalculadoraFiscal.AplicarIva(resultado.Data.precioProducto);
             
               
                if (resultado.Success)
                {
                    // ============================================
                    // 7. NOTIFICACIÓN ASÍNCRONA 
                    // ============================================
                    _background.Enqueue(async (sp, ct) =>
                    {
                        var notificar = sp.GetRequiredService<IRefundNotification>();
                        await notificar.EnviarEmailNotificacionRembolso(
                            resultado.Data.pedidoId,
                           total,
                           resultado.Data.motivo);
                    });
                    return Json(new { success = true, message = "rembolso realizado con exito" });
                }
                else
                {
                    return Json(new { success = false, message = "Ocurrio un error al realizar el rembolso" });
                }

               

            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validación fallida en reembolso parcial pedido {PedidoId}", request.DetalleId);
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en reembolso parcial pedido {PedidoId}", request.DetalleId);
                return Json(new { success = false, message = "No se pudo realizar el reembolso. Intenta de nuevo o contacta soporte." });
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
                var dto = new RefundDto
                {
                    NumeroPedido = form.NumeroPedido,
                    NombreCliente = form.NombreCliente,
                    EmailCliente = form.EmailCliente,
                    FechaRembolso = form.FechaRembolso,
                    MotivoRembolso = form.MotivoRembolso,
                };

                var obtenerNumeroPedido = await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.ObtenerNumeroPedido(dto));

                if (obtenerNumeroPedido == null)
                {
                    _logger.LogInformation("El numero de pedido proporcionado no existe " + obtenerNumeroPedido);
                    return RedirectToAction(nameof(FormularioRembolso));
                }

                int usuarioActual = _currentUserAccessor.GetCurrentUserId();

                var emailCliente = _policyExecutor.ExecutePolicy(() => _currentUserAccessor.GetCurrentUserEmail());
                if (emailCliente == null)
                {
                    _logger.LogInformation("El email proporcionado no se encuentra registrado " + emailCliente);
                }


                var pedido = await _policyExecutor.ExecutePolicyAsync(() => _pedidoRepository.ObtenerNumeroPedido(dto));

                if (pedido == null)
                {

                    _logger.LogInformation("El pedido con el numero de pedido proporcionado no existe ");
                    return View(nameof(FormularioRembolso));
                }
                var capture = pedido.PayPalPaymentCaptures?.FirstOrDefault();
                var orderId = capture?.PaymentId;
                var detallespago = await _policyExecutor.ExecutePolicyAsync(() =>
                    _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(orderId));

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

                var paymentDetail = _mappingService.MapearOrdenADetallePago(detallespago);
              
                // Lista para almacenar los ítems de PayPal
                var paypalItems = await _paymentService.ProcesarRembolso(firstPurchaseUnit, paymentDetail, usuarioActual, dto, obtenerNumeroPedido, emailCliente);
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
