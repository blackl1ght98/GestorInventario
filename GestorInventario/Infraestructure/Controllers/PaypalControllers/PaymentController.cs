using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.ViewModels.Paypal;
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
        private readonly IPaypalRepository _paypalRepository;
        private readonly IBackgroundTaskQueue _background;
        
        public PaymentController(
            ILogger<PaymentController> logger,   
            ICurrentUserAccessor currentUser,       
            IPolicyExecutor policyExecutor, 
            IPaypalOrderService paypalOrderService,     
            IPedidoManagementService pedidoService, 
            IPaymentService paymentService,
            IPaypalRepository repo,
            IBackgroundTaskQueue background)
        {
            _logger = logger;           
            _policyExecutor = policyExecutor;
            _paypalOrderService = paypalOrderService;             
            _currentUserAccessor = currentUser;  
            _pedidoService = pedidoService;
            _paymentService = paymentService;
            _paypalRepository = repo;
            _background = background;
           
           
        }
        
        public async Task<IActionResult> Success(string token=null)
        {
            try
            {
                var paymentId = token ?? string.Empty;

                // Capturamos el pago en PayPal y guardamos el resultado en BD
                // (pedido Pagado, payment detail y capture con PENDING_SYNC).
                var (captureId, total, currency) = await _paypalOrderService.CapturarPagoAsync(paymentId);

                var usuarioActual = _currentUserAccessor.GetCurrentUserId();
                var result = await _pedidoService.ConfirmarPagoDelPedidoAsync(usuarioActual, captureId, total, currency, paymentId);

                if (result.Success)
                {
                    // Tras confirmar, sincronizamos los datos reales con PayPal y
                    // llevamos al usuario directamente al detalle de su pago.
                    return RedirectToAction(nameof(Sincronizar), new { paymentId = paymentId, pedidoId = result.Data?.Id ?? 0 });
                }
                else
                {
                    return RedirectToAction("Error", "Home");
                }
            }
            catch (Exception ex)
            {
                TempData["ConectionError"] = "El servidor ha tardado mucho en responder, inténtelo de nuevo más tarde";
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

                var resultado = await _policyExecutor.ExecutePolicyAsync(() => _paymentService.ReintentarPago(pedidoId));
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
                TempData["ConectionError"] = "El servidor a tardado mucho en responder intentelo de nuevo mas tarde";
                _logger.LogError(ex, "Error al realizar el checkout");
                return RedirectToAction("Error", "Home");
            }

        }
        [Authorize]
        public async Task<IActionResult> Cancel()
        {
            return RedirectToAction("Index", "Productos");
        }

        // Sobrescribe los datos de la BD con los reales de PayPal.
        // Pensado para llamarse tras un pago (Success lo encadena) o manualmente
        // desde un botón "Sincronizar" en la vista de detalle.
        // Termina redirigiendo a DetallesPagoEjecutado para mostrar la factura ya actualizada.
        [Authorize]
        public async Task<IActionResult> Sincronizar(string paymentId, int pedidoId)
        {

            var currentUser = _currentUserAccessor.GetCurrentUserId();
            // Encolamos la sincronización para no hacer esperar al usuario.
            // El callback se ejecuta dentro de un scope nuevo de DI, por lo que las
            // dependencias (DbContext, IPedidoManagementService, etc.) se resuelven ahí.
            _background.Enqueue(async (sp, ct) =>
            {
                // Resolvemos IPedidoManagementService desde el scope del worker, no del controller.
                var pedidoService = sp.GetRequiredService<IPedidoManagementService>();
                var notificationService = sp.GetRequiredService<INotificationService>();
               
                var logger = sp.GetRequiredService<ILogger<PaymentController>>();
          
            
                try
                {
                    var result = await pedidoService.SincronizarDetallePagoAsync(paymentId, pedidoId);
                    if (!result.Success || result.Data == null)
                    {
                        logger.LogError("Sincronización en background fallida para pago {PaymentId}: {Message}",
                            paymentId, result.Message);
                    }
                    else
                    {
                     logger.LogInformation("Sincronización en background completada para pago {PaymentId}", paymentId);
                     await  notificationService.CrearNotificacion(currentUser,"Sincronizacion completada", "Sicronizacion completada con exito", "INFO");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Excepción al sincronizar en background el pago {PaymentId}", paymentId);
                }
            });

            // Volvemos al Index de pedidos inmediatamente, sin esperar a PayPal.
            return RedirectToAction("Index", "Pedidos");
        }


        [Authorize]
        public async Task<IActionResult> DetallesPagoEjecutado(string paymentId)
        {
            // Lee los datos directamente de la BD (sin tocar PayPal).
            // Si necesitas datos frescos de PayPal, llama antes a Sincronizar.
            var paypalDetail = await _paypalRepository.ObtenerDetallePagoPorId(paymentId);

            if (paypalDetail == null)
            {
                _logger.LogError("No se encontró el detalle de pago {PaymentId} en BD", paymentId);
                return RedirectToAction("Error", "Home");
            }

            var ultimoCapture = paypalDetail.PayPalPaymentCaptures?
                .OrderByDescending(c => c.Id)
                .FirstOrDefault();

            var ultimaInfoEnvio = paypalDetail.PayPalPaymentShippings
                .OrderByDescending(c => c.Id)
                .FirstOrDefault();

            var viewModel = new PayPalPaymentDetailViewModel
            {
                // Datos del pagador
                Id = paypalDetail.Id,
                Intent = paypalDetail.Intent,
                Status = paypalDetail.OrderStatus,
                CreateTime = paypalDetail.CreateTime,
                UpdateTime = paypalDetail.UpdateTime,

                PayerEmail = paypalDetail.PayerEmail,
                PayerFirstName = paypalDetail.PayerFirstName,
                PayerLastName = paypalDetail.PayerLastName,
                PayerId = paypalDetail.PayerId,

                // Datos de envío
                ShippingRecipientName = ultimaInfoEnvio?.RecipientName,
                ShippingLine1 = ultimaInfoEnvio?.AddressLine1,
                ShippingCity = ultimaInfoEnvio?.City,
                ShippingState = ultimaInfoEnvio?.State,
                ShippingPostalCode = ultimaInfoEnvio?.PostalCode,
                ShippingCountryCode = ultimaInfoEnvio?.CountryCode,

                // Importe
                AmountTotal = paypalDetail.AmountTotal,
                AmountCurrency = paypalDetail.AmountCurrency,
                AmountItemTotal = paypalDetail.AmountItemTotal,
                AmountShipping = paypalDetail.AmountShipping,

                // Datos vendedor
                PayeeMerchantId = paypalDetail.PayeeMerchantId,
                PayeeEmail = paypalDetail.PayeeEmail,
                Description = paypalDetail.Description,

                // Datos del pago
                SaleId = ultimoCapture?.CaptureId,
                CaptureStatus = ultimoCapture?.Status,
                CaptureAmount = ultimoCapture?.Amount,
                CaptureCurrency = ultimoCapture?.Currency,
                ProtectionEligibility = ultimoCapture?.ProtectionEligibility,
                TransactionFeeAmount = ultimoCapture?.TransactionFeeAmount,
                TransactionFeeCurrency = ultimoCapture?.TransactionFeeCurrency,
                ReceivableAmount = ultimoCapture?.ReceivableAmount,
                ReceivableCurrency = ultimoCapture?.ReceivableCurrency,
                ExchangeRate = ultimoCapture?.ExchangeRate,
                FinalCapture = ultimoCapture?.FinalCapture,
                DisputeCategories = ultimoCapture?.DisputeCategories,

                PayPalPaymentItems = paypalDetail.PayPalPaymentItems?.Select(item => new PayPalPaymentItemViewModel
                {
                    ItemName = item.ItemName,
                    ItemSku = item.ItemSku,
                    ItemPrice = item.ItemPrice,
                    ItemCurrency = item.ItemCurrency,
                    ItemTax = item.ItemTax,
                    ItemQuantity = item.ItemQuantity
                }).ToList() ?? new List<PayPalPaymentItemViewModel>()
            };

            return View(viewModel);
        }
    }

}      
