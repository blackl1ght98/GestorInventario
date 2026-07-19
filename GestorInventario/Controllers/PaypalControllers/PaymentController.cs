
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.DTOS.Paypal.BD;
using GestorInventario.Shared.Utilities;

using GestorInventario.ViewModels.Paypal;
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
            var ultimoRefund = paypalDetail.PayPalPaymentRefunds?
            .OrderByDescending(r => r.CreateTime)
            .FirstOrDefault();

            var viewModel = new PayPalPaymentDetailViewModel
            {
                Id = paypalDetail.Id,
                Intent = paypalDetail.Intent,
                Status = paypalDetail.OrderStatus,
                CreateTime = paypalDetail.CreateTime,
                UpdateTime = paypalDetail.UpdateTime,

                Payer = new PayerInfo
                {
                    Email = paypalDetail.PayerEmail,
                    FirstName = paypalDetail.PayerFirstName,
                    LastName = paypalDetail.PayerLastName,
                    PayerId = paypalDetail.PayerId,
                },

                Shipping = new ShippingInfo
                {
                    RecipientName = ultimaInfoEnvio?.RecipientName ?? string.Empty,
                    Line1 = ultimaInfoEnvio?.AddressLine1 ?? string.Empty,
                    City = ultimaInfoEnvio?.City ?? string.Empty,
                    State = ultimaInfoEnvio?.State ?? string.Empty,
                    PostalCode = ultimaInfoEnvio?.PostalCode ?? string.Empty,
                    CountryCode = ultimaInfoEnvio?.CountryCode ?? string.Empty,
                },

                Amount = new AmountInfo
                {
                    Total = paypalDetail.AmountTotal,
                    Currency = paypalDetail.AmountCurrency ?? string.Empty,
                    ItemTotal = paypalDetail.AmountItemTotal ,
                    Shipping = paypalDetail.AmountShipping ,
                    Tax = paypalDetail.AmountTax,
                },

                Payee = new PayeeInfo
                {
                    MerchantId = paypalDetail.PayeeMerchantId ?? string.Empty,
                    Email = paypalDetail.PayeeEmail ?? string.Empty,
                    Description = paypalDetail.Description ?? string.Empty,
                },

                Capture = new CaptureInfo
                {
                    SaleId = ultimoCapture?.CaptureId ?? string.Empty,
                    Status = ultimoCapture?.Status ?? string.Empty,
                    Amount = ultimoCapture?.Amount ?? 0m,
                    Currency = ultimoCapture?.Currency ?? string.Empty,
                    ProtectionEligibility = ultimoCapture?.ProtectionEligibility ?? string.Empty,
                    TransactionFeeAmount = ultimoCapture?.TransactionFeeAmount ?? 0m,
                    TransactionFeeCurrency = ultimoCapture?.TransactionFeeCurrency ?? string.Empty,
                    ReceivableAmount = ultimoCapture?.ReceivableAmount ?? 0m,
                    ReceivableCurrency = ultimoCapture?.ReceivableCurrency ?? string.Empty,
                    ExchangeRate = ultimoCapture?.ExchangeRate ?? 0m,
                    FinalCapture = ultimoCapture?.FinalCapture ?? false,
                    DisputeCategories = ultimoCapture?.DisputeCategories ?? string.Empty,
                },
                Refund = ultimoRefund != null
                  ? new RefundInfo
                  {
                      RefundId = ultimoRefund.RefundId,
                      Status = ultimoRefund.Status,
                      Amount = ultimoRefund.Amount,
                      Currency = ultimoRefund.Currency,
                      NoteToPayer = ultimoRefund.NoteToPayer,
                      CreateTime = ultimoRefund.CreateTime,
                      UpdateTime = ultimoRefund.UpdateTime,
                  }
                  : null,
                Items = paypalDetail.PayPalPaymentItems?.Select(item => new PayPalPaymentItemDto
                {
                    ItemName = item.ItemName,
                    ItemSku = item.ItemSku,
                    ItemPrice = item.ItemPrice,
                    ItemCurrency = item.ItemCurrency,
                    ItemTax = item.ItemTax,
                    ItemQuantity = item.ItemQuantity,
                }).ToList() ?? new List<PayPalPaymentItemDto>(),
            };

            return View(viewModel);
        }
    }

}      
