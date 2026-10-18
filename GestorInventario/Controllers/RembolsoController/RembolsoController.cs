using GestorInventario.Application.DTOS.Paypal.BD;
using GestorInventario.Application.DTOS.Rembolso;
using GestorInventario.Application.Interfaces.Application.Services.BackgroundServices;
using GestorInventario.Application.Interfaces.Application.Services.Payment;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Refunds;
using GestorInventario.Application.Interfaces.Application.Services.Refunds;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Application.Interfaces.Infrastructure.RetryPolicy;
using GestorInventario.Application.Interfaces.MetodosPaginacion;
using GestorInventario.Application.Interfaces.Notifications.SendNotification.Email;
using GestorInventario.Application.Interfaces.Web;
using GestorInventario.Application.Services.Common;
using GestorInventario.Domain.Common;
using GestorInventario.Extensions;
using GestorInventario.Shared.Utilities;
using GestorInventario.ViewModels.Paypal;
using GestorInventario.ViewModels.Refunds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace GestorInventario.Controllers.RembolsoController
{
   
    public class RembolsoController : Controller
    {
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IRembolsoRepository _rembolsoRepository;       
        private readonly ILogger<RembolsoController> _logger;
        private readonly IPaginationService _paginationService;     
        private readonly IPedidoRepository _pedidoRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IPaypalOrderService _paypalOrderService;
        private readonly IPaymentService _paymentService;
        
        private readonly IRefundService _refundService;
        private readonly IBackgroundTaskQueue _background;
      
        private readonly IPaypalRefundService _paypalRefundService;
        public RembolsoController(
            IPolicyExecutor policyExecutor, 
            IRembolsoRepository rembolsoRepository, 
             ILogger<RembolsoController> logger, 
             IPaginationService paginationService,      
             IPedidoRepository pedidoRepository,
             ICurrentUserAccessor currentUserAccessor,
             IPaypalOrderService paypalOrderService,
             IPaymentService paymentService,
            
             IRefundService refundService,
             IBackgroundTaskQueue provider,   
             IPaypalRefundService refund)
        {
            _policyExecutor = policyExecutor;
            _rembolsoRepository = rembolsoRepository;  
            _logger = logger;
            _paginationService = paginationService;
            _pedidoRepository = pedidoRepository;
            _currentUserAccessor = currentUserAccessor;
            _paypalOrderService = paypalOrderService;
            _paymentService = paymentService;
                 
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
                    _paginationService.PaginarAsync(queryable, paginacion)
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

                // ============================================
                // 2. CALCULAR MONTO A REEMBOLSAR (todas las líneas
                //    aún no reembolsadas, según BD, con su cantidad real)
                // ============================================
                
                var detallesPendientes = pedido.DetallePedidos
                    .Where(d => !(d.Rembolsado ))
                    .ToList();

                if (!detallesPendientes.Any())
                    return BadRequest("No quedan líneas pendientes de reembolso en este pedido");

                var (_, _, montoSolicitadoConIva) = CalculadoraFiscal.CalcularTotales(
                    detallesPendientes.Select(d => (d.Producto.Precio, d.Cantidad))
                );

                _logger.LogInformation(
                    "Reembolso total pedido {PedidoId} -> Líneas pendientes:{Cantidad} Monto solicitado:{Monto}",
                    request.PedidoId, detallesPendientes.Count, montoSolicitadoConIva);

                // ============================================
                // 3. VERIFICAR CONTRA PAYPAL (fuente de verdad real)
                // ============================================

                var captureDetails = await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(
                    pedido.PayPalPaymentCaptures.First().PaymentId);
                var details = captureDetails.Unwrap();
    
                var resultRefund = PaypalRefundCalculator.CalcularMontoDisponibleYEstado(
                    details, montoSolicitadoConIva, request.Currency);
                var finalRefund = resultRefund.Unwrap();

                var refundResult = await _paypalRefundService.RefundCaptureAsync(
                        captureId: captureId,
                        amount: finalRefund.MontoReembolso,
                        currency: request.Currency,
                        nota: $"Reembolso pedido #{pedido.NumeroPedido}");

                if (!refundResult.Success)
                    return BadRequest(new { success = false, message = refundResult.Message });

                var procesar = await _refundService.ProcesarRembolsoTotalAsync(
                        pedido.Id,
                        refundResult.Data.RefundId,
                        refundResult.Data.AmountRefunded);
                
                if (procesar.Success)
                {
                    var detalleIds = detallesPendientes.Select(d => d.Id).ToList();
                    _background.Enqueue(async (sp, ct) =>
                    {
                        var notificar = sp.GetRequiredService<IRefundNotification>();
                        await notificar.EnviarEmailNotificacionRembolso(
                            pedido.Id,
                            detalleIds,
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
               
                var total = CalculadoraFiscal.CalcularPrecioConIva(resultado.Data.precioProducto);
             
               
                if (resultado.Success)
                {
                  
                    _background.Enqueue(async (sp, ct) =>
                    {
                        var notificar = sp.GetRequiredService<IRefundNotification>();
                        await notificar.EnviarEmailNotificacionRembolso(
                            resultado.Data.pedidoId,
                            new[] { resultado.Data.detalleId },
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

        var obtenerNumeroPedido = await _policyExecutor.ExecutePolicyAsync(
            () => _pedidoRepository.ObtenerNumeroPedido(dto.NumeroPedido));

        if (obtenerNumeroPedido == null)
        {
            _logger.LogInformation(
                "El número de pedido proporcionado no existe: {NumeroPedido}", dto.NumeroPedido);
            return RedirectToAction(nameof(FormularioRembolso));
        }

        int usuarioActual = _currentUserAccessor.GetCurrentUserId();

        var emailCliente = _policyExecutor.ExecutePolicy(
            () => _currentUserAccessor.GetCurrentUserEmail());

        if (string.IsNullOrEmpty(emailCliente))
        {
            _logger.LogInformation("El email del usuario actual no se encuentra registrado");
        }

        var pedido = await _policyExecutor.ExecutePolicyAsync(
            () => _pedidoRepository.ObtenerNumeroPedido(dto.NumeroPedido));

        if (pedido == null)
        {
            _logger.LogInformation(
                "El pedido con el número proporcionado no existe: {NumeroPedido}", dto.NumeroPedido);
            return View(nameof(FormularioRembolso));
        }

        var capture = pedido.PayPalPaymentCaptures?.FirstOrDefault();
        var orderId = capture?.PaymentId;

        if (string.IsNullOrEmpty(orderId))
        {
            _logger.LogWarning(
                "El pedido {NumeroPedido} no tiene PaymentId de PayPal", dto.NumeroPedido);
            return View(nameof(FormularioRembolso));
        }

        var detallespago = await _policyExecutor.ExecutePolicyAsync(
            () => _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(orderId));

        if (!detallespago.Success)
        {
            _logger.LogError(
                "Error al obtener detalles de PayPal para orderId {OrderId}: {Message}",
                orderId, detallespago.Message);
            return StatusCode(500, "No se pudieron obtener los detalles del pago");
        }

        var detalles = detallespago.Unwrap(); // PayPalOrderDetailsDto

        // Ya no hace falta comprobar PurchaseUnits ni mapear aquí.
        // El DTO limpio ya trae Items, Captures, etc.
        var resultado = await _paymentService.ProcesarRembolso(
            detalles,          
            usuarioActual,
            dto,
            obtenerNumeroPedido,
            emailCliente);

        if (!resultado.Success)
        {
            _logger.LogError(
                "Error al procesar reembolso del pedido {NumeroPedido}: {Message}",
                dto.NumeroPedido, resultado.Message);
            return StatusCode(500, resultado.Message);
        }

        if (User.IsAdministrador())
            return RedirectToAction("Index", "Admin");

        return RedirectToAction("Index", "Pedido");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al procesar el reembolso");
        return StatusCode(500, "Ocurrió un error al procesar tu solicitud");
    }
}

    }
}
