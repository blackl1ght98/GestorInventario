using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Application.Interfaces.Application.Services.Orders;
using GestorInventario.Application.Interfaces.Application.Services.Payment;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Application.Interfaces.Infrastructure.RetryPolicy;
using GestorInventario.Application.Interfaces.Notifications.InternalNotification;
using GestorInventario.Application.Interfaces.Web;
using GestorInventario.Domain.enums.Notification;
using GestorInventario.Mappers;
using GestorInventario.Shared.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace GestorInventario.Controllers.PaypalControllers
{
    public class PaymentController : Controller
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IPaypalOrderService _paypalOrderService;
        private readonly IPolicyExecutor _policyExecutor;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IOrderService _pedidoService;
        private readonly IPaymentService _paymentService;
        private readonly IPaypalRepository _paypalRepository;
        private readonly INotificationService _notificationService;

        public PaymentController(
            ILogger<PaymentController> logger,
            ICurrentUserAccessor currentUser,
            IPolicyExecutor policyExecutor,
            IPaypalOrderService paypalOrderService,
            IOrderService pedidoService,
            IPaymentService paymentService,
            IPaypalRepository repo,
            INotificationService noti)
        {
            _logger = logger;
            _policyExecutor = policyExecutor;
            _paypalOrderService = paypalOrderService;
            _currentUserAccessor = currentUser;
            _pedidoService = pedidoService;
            _paymentService = paymentService;
            _paypalRepository = repo;
            _notificationService = noti;
        }

        public async Task<IActionResult> Success(string token)
        {
            try
            {
                var paymentId = token ?? string.Empty;
                var finalResult = await _paypalOrderService.CapturarPagoAsync(paymentId);
                var pago = finalResult.Unwrap();
                var captureId = pago?.Captures?
                    .FirstOrDefault()?.CaptureId;
                var amount = pago.Captures.FirstOrDefault()?.Amount;
                var currency = pago.Captures.FirstOrDefault()?.Currency;
                var captured = new PaymentCapturedDto
                {
                    CaptureId = captureId,
                    Total = (decimal)amount,
                    Currency = currency
                };
                var confirmar = await _pedidoService.ConfirmarPagoDelPedidoAsync(captured, paymentId);
                var result = confirmar.Unwrap();
                if (finalResult.Success)
                {
                    // Tras confirmar, sincronizamos los datos reales con PayPal y
                    // llevamos al usuario directamente al detalle de su pago.
                    return RedirectToAction(nameof(Sincronizar), new { paymentId = paymentId, pedidoId = result.Id });
                }
                else
                {
                    return RedirectToAction("Error", "Home");
                }
            }
            catch (Exception ex)
            {
               
                _logger.LogError(ex, "Error al realizar el pago");
                return RedirectToAction("Error", "Home");
            }
        }

        [Authorize]
        public async Task<IActionResult> ReintentarPago(int pedidoId)
        {
            CultureHelper.SetInvariantCulture();
            try
            {
                var resultado =
                    await _policyExecutor.ExecutePolicyAsync(() => _paymentService.ReintentarPago(pedidoId));
                if (resultado.Success)
                {
                    return Redirect(resultado.Data);
                }
                else
                {
                    _logger.LogError("Ocurrio un error al redireccionar a paypal");
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
               
                _logger.LogError(ex, "Error al realizar el checkout");
                return RedirectToAction("Error", "Home");
            }
        }

        [Authorize]
        public async Task<IActionResult> Cancel()
        {
            return RedirectToAction("Index", "Productos");
        }

        [Authorize]
        public async Task<IActionResult> Sincronizar(string paymentId, int pedidoId)
        {
            await EjecutarSincronizacionAsync(paymentId, pedidoId);
            return RedirectToAction("Index", "Pedidos");
        }

        [Authorize]
        public async Task<IActionResult> SincronizarJs(string paymentId, int pedidoId)
        {
            var result = await EjecutarSincronizacionAsync(paymentId, pedidoId);

            if (!result.Success || result.Data == null)
                return BadRequest(new { success = false, message = result.Message });

            return Ok(new { success = true, message = result.Message });
        }

        private async Task<OperationResult<string>> EjecutarSincronizacionAsync(string paymentId, int pedidoId)
        {
            var currentUser = _currentUserAccessor.GetCurrentUserId();

            try
            {
                var result = await _pedidoService.SincronizarDetallePagoAsync(paymentId, pedidoId);

                if (!result.Success || result.Data == null)
                {
                    _logger.LogError("Sincronización fallida para pago {PaymentId}: {Message}",
                        paymentId, result.Message);
                }
                else
                {
                    _logger.LogInformation("Sincronización completada para pago {PaymentId}", paymentId);
                    await _notificationService.CrearNotificacion(
                        currentUser, "Sincronizacion completada", "Sincronizacion completada con exito",
                        TipoNotificacion.Info);
                }

                return OperationResult<string>.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción al sincronizar el pago {PaymentId}", paymentId);
                return OperationResult<string>.Fail("Error procesando la sincronización");
            }
        }


        [Authorize]
        public async Task<IActionResult> DetallesPagoEjecutado(string paymentId)
        {
            var paypalDetail = await _paypalRepository.ObtenerDetallePagoPorId(paymentId);

            if (paypalDetail == null)
            {
                _logger.LogError("No se encontró el detalle de pago {PaymentId} en BD", paymentId);
                return RedirectToAction("Error", "Home");
            }

            var viewModel = PayPalPaymentMapper.ToViewModel(paypalDetail);
            return View(viewModel);
        }
    }
}