using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.ViewModels.Paypal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;



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
            IPaymentService paymentService
          
           )
        {
            _logger = logger;           
            _policyExecutor = policyExecutor;
            _paypalOrderService = paypalOrderService;             
            _currentUserAccessor = currentUser;  
            _pedidoService = pedidoService;
            _paymentService = paymentService;
            
           
           
        }
        
        public async Task<IActionResult> Success(string token=null)
        {
            try
            {
                
                var orderId = token ?? string.Empty;
                //CaptureId->representa el id del pago en paypal
                //total-> lo que has pagado
                //currency-> la moneda
                
               var (captureId, total, currency) = await _paypalOrderService.CapturarPagoAsync(orderId);
                

               var usuarioActual = _currentUserAccessor.GetCurrentUserId();
 
               var result = await _pedidoService.ConfirmarPagoDelPedidoAsync(usuarioActual,captureId,total,currency,orderId);
                if (result.Success)
                {
                    return RedirectToAction("Index", "Pedidos");
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
        [Authorize]
        public async Task<IActionResult> DetallesPagoEjecutado(string id, int pedidoId)
        {
            var result = await _policyExecutor.ExecutePolicyAsync(() => _pedidoService.SincronizarDetallePagoAsync(id, pedidoId));

            if (!result.Success || result.Data == null)
            {
                _logger.LogError(result.Message);
                return RedirectToAction(nameof(Index));
            }

            var paypalDetail = result.Data;

            var ultimoCapture = paypalDetail.PayPalPaymentCaptures?
                .OrderByDescending(c => c.Id)
                .FirstOrDefault();

            // ✅ Proteger contra null
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

                // ✅ Datos de envío con protección null
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

                // ✅ Datos del pago con protección null
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

                TrackingId = paypalDetail.TrackingId,
                TrackingStatus = paypalDetail.TrackingStatus,

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
