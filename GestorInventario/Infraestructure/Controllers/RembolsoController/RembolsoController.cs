using GestorInventario.Application.DTOs.Paypal.Responses.GET.Order;
using GestorInventario.Application.DTOs.Rembolso;
using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Rembolso;
using GestorInventario.Application.Services.Common;
using GestorInventario.Domain.Models;
using GestorInventario.enums.Pedido;

using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.MetodosExtension;
using GestorInventario.PaginacionLogica;
using GestorInventario.Utilities;
using GestorInventario.ViewModels.Paypal;
using GestorInventario.ViewModels.Rembolsos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Globalization;

namespace GestorInventario.Infraestructure.Controllers.RembolsoController
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
        private readonly IPedidoManagementService _pedidoService;
        private readonly IBackgroundTaskQueue _background;
      
        private readonly IPaypalRefundService _refundService;
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
             IPedidoManagementService pedido,
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
            _pedidoService = pedido;
            _background = provider;
            _refundService = refund;


        }
        [Authorize(Roles = "Administrador")]
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

                var viewModel = new RembolsosViewModel
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
        [Authorize(Roles = "Administrador")]
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
        [Authorize(Roles ="Administrador")]
        public async Task<IActionResult> RefundSale([FromBody] RefundFullDto request)
        {
            if (request == null || request.PedidoId <= 0)
                return BadRequest("Datos inválidos");

            try
            {
                // 1. TU dominio: leer pedido de TU base de datos
                var pedido = await _pedidoRepository
                    .ObtenerPedidoConDetallesAsync(request.PedidoId);
                
                if (pedido == null)
                    return NotFound("Pedido no encontrado");
                // 2. Idempotencia: si ya hay un reembolso exitoso, no procesamos otro
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

               
                    // 2. Llamar al servicio de PayPal con datos planos (sin BD)
                    var refundResult = await _refundService.RefundCaptureAsync(
                        captureId: captureId,
                        amount: totalReembolso,
                        currency: request.Currency,
                        nota: $"Reembolso pedido #{pedido.NumeroPedido}");

                    if (!refundResult.Success)
                        return BadRequest(new { success = false, message = refundResult.Message });

                    // 3. Agregamos la informacion a la tabla rembolso
                    await _pedidoService.ProcesarRembolsoAsync(
                        pedido.Id,
                        EstadoPedido.Rembolsado.ToString(),
                        refundResult.Data.RefundId);

                    // 4. Notificación asíncrona
                    _background.Enqueue(async (sp, ct) =>
                    {
                        var notificar = sp.GetRequiredService<IPaypalService>();
                        await notificar.EnviarEmailNotificacionRembolso(
                            pedido.Id,
                            refundResult.Data.AmountRefunded,
                            "Reembolso Aprobado");
                    });

                    return Ok(new { success = true, refundId = refundResult.Data.RefundId });
               
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en refund pedido {PedidoId}", request.PedidoId);
                return StatusCode(500, new { success = false, message = "Error procesando reembolso" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles ="Administrador")]
        public async Task<IActionResult> RefundPartial([FromBody] RefundPartialDto request)
        {
            if (request?.DetalleId <= 0)
            {
                return Json(new { success = false, message = "Solicitud inválida." });
            }

            try
            {
                // ============================================
                // 1. OBTENER DATOS DEL PEDIDO (tu BD)
                // ============================================
                var detallePedido = await _pedidoRepository.ObtenerDetalleParaReembolsoAsync(request.DetalleId);
                if (detallePedido == null)
                    return Json(new { success = false, message = "Pedido no encontrado." });

                // ============================================
                // 2. CALCULAR MONTO CON IVA 
                // ============================================
                var precioSinIva = detallePedido.Producto.Precio;
                var ivaUnitario = CalculadoraFiscal.CalcularIvaUnitario(precioSinIva);
                var montoSolicitadoConIva = precioSinIva + ivaUnitario;

                _logger.LogInformation(
                    "Reembolso parcial pedido {PedidoId} -> Precio:{Precio} IVA:{Iva} Total:{Total}",
                    request.DetalleId, precioSinIva, ivaUnitario, montoSolicitadoConIva);

                // ============================================
                // 3. VERIFICAR ESTADO ACTUAL EN PAYPAL
                // ============================================
                var captureDetails = await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(detallePedido.Pedido.PayPalPaymentCaptures.First().PaymentId);
                var (montoReembolso, montoDisponible, estadoVenta) = CalcularMontoDisponibleYEstado(
                    captureDetails, montoSolicitadoConIva, request.Currency);

                // ============================================
                // 4. EJECUTAR REEMBOLSO EN PAYPAL 
                // ============================================
                var refundResult = await _refundService.RefundCaptureAsync(
                    captureId: detallePedido.Pedido.PayPalPaymentCaptures.First().CaptureId,
                    amount: montoReembolso,
                    currency: detallePedido.Pedido.Currency,
                    nota: $"Reembolso parcial pedido #{detallePedido.Pedido.Id} - {request.Motivo}");

                if (!refundResult.Success)
                {
                    // ============================================
                    // 5. MANEJO DE FALSO POSITIVO 
                    // ============================================
                    if (refundResult.Message.Contains("REFUND_AMOUNT_EXCEEDED") ||
                        refundResult.Message.Contains("UnprocessableEntity"))
                    {
                        var updatedCapture = await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(detallePedido.Pedido.PayPalPaymentCaptures.First().PaymentId);
                        var montoFormateado = CalculadoraFiscal.FormatearPayPal(montoSolicitadoConIva);

                        var recentRefund = updatedCapture?.PurchaseUnits[0].Payments.Refunds?
                            .FirstOrDefault(r => r.Amount.Value == montoFormateado);

                        if (recentRefund != null)
                        {
                            _logger.LogWarning("Falso positivo: Reembolso ya procesado (ID {RefundId}).", recentRefund.Id);

                            // Usar el refundId existente como si hubiera funcionado
                            refundResult = OperationResult<(string, decimal)>.Ok(
                                "Reembolso ya existente",
                                (recentRefund.Id, montoReembolso));
                        }
                        else
                        {
                            return Json(new
                            {
                                success = false,
                                message = $"El monto ({montoSolicitadoConIva} {request.Currency}) excede disponible ({montoDisponible} {request.Currency})."
                            });
                        }
                    }
                    else
                    {
                        return Json(new { success = false, message = refundResult.Message });
                    }
                }

                // ============================================
                // 6. REGISTRAR EN TU BASE DE DATOS 
                // ============================================
                await _pedidoService.RegistrarReembolsoParcialAsync(
                    detallePedido.Pedido.Id,
                    detallePedido.Id,            
                    request.Motivo,
                    montoReembolso,
                    detallePedido.Pedido.Currency,
                    refundResult.Data.RefundId
                    );

                // ============================================
                // 7. NOTIFICACIÓN ASÍNCRONA 
                // ============================================
                _background.Enqueue(async (sp, ct) =>
                {
                    var notificar = sp.GetRequiredService<IPaypalService>();
                    await notificar.EnviarEmailNotificacionRembolso(
                        detallePedido.Pedido.Id,
                        detallePedido.Producto.Precio,
                        request.Motivo);
                });

                return Json(new { success = true, refundId = refundResult.Data.RefundId });
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
        private (decimal montoReembolso, decimal montoDisponible, string estadoVenta)
         CalcularMontoDisponibleYEstado(
             OrderDetailsResponse captureDetails,
             decimal montoSolicitado,
             string currency)
        {
            var firstUnit = captureDetails.PurchaseUnits?.FirstOrDefault()
                ?? throw new InvalidOperationException("La orden no contiene unidades de compra.");

            var capture = firstUnit.Payments?.Captures?.FirstOrDefault()
                ?? throw new InvalidOperationException("La orden no contiene capturas de pago.");

            if (currency != capture.Amount?.CurrencyCode)
            {
                throw new InvalidOperationException(
                    $"Moneda solicitada ({currency}) no coincide con la captura ({capture.Amount?.CurrencyCode}).");
            }

            // Parseo seguro del net amount
            var netAmount = ParseDecimalSeguro(
                capture.SellerReceivableBreakdown?.NetAmount?.Value,
                "monto neto de la captura");

            // Suma de reembolsos previos
            var refundedAmount = firstUnit.Payments?.Refunds?
                .Where(r => r.SellerPayableBreakdown?.NetAmount?.Value != null)
                .Sum(r => ParseDecimalSeguro(r.SellerPayableBreakdown.NetAmount.Value, "monto de reembolso previo"))
                ?? 0m;

            var availableAmount = netAmount - refundedAmount;

            if (availableAmount <= 0)
            {
                _logger.LogWarning("No hay fondos disponibles para reembolsar. Net: {Net}, Ya reembolsado: {Refunded}",
                    netAmount, refundedAmount);
                throw new InvalidOperationException("No hay monto disponible para reembolsar.");
            }

            // Ajustar monto solicitado al disponible
            var finalRefundAmount = Math.Min(montoSolicitado, availableAmount);

            if (finalRefundAmount < montoSolicitado)
            {
                _logger.LogWarning(
                    "Monto solicitado ({Solicitado}) excede disponible ({Disponible}). Ajustando a {Ajustado}.",
                    montoSolicitado, availableAmount, finalRefundAmount);
            }

            // Estado: si reembolsamos todo lo disponible, es refund completo. Si no, parcial.
            var estadoVenta = finalRefundAmount >= availableAmount && refundedAmount == 0
                ? "REFUNDED"
                : "PARTIALLY_REFUNDED";

            return (finalRefundAmount, availableAmount, estadoVenta);
        }
        private static decimal ParseDecimalSeguro(string? value, string campo)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"El campo '{campo}' no contiene un valor válido.");
            }

            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                throw new InvalidOperationException($"No se pudo parsear el campo '{campo}': {value}");
            }

            return result;
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
