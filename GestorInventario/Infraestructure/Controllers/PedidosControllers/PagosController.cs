using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.ViewModels.Paypal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestorInventario.Infraestructure.Controllers.PedidosControllers
{
    public class PagosController : Controller
    {
        private readonly IPolicyExecutor _policyExecutor;
        private readonly IPedidoManagementService _pedidoService;
        private readonly ILogger<PagosController> _logger;

        public PagosController(IPolicyExecutor policyExecutor, IPedidoManagementService pedidoService, ILogger<PagosController> logger)
        {
            _policyExecutor = policyExecutor;
            _pedidoService = pedidoService;
            _logger = logger;
        }

        [Authorize]
        public async Task<IActionResult> DetallesPagoEjecutado(string id)
        {
            var result = await _policyExecutor.ExecutePolicyAsync(() => _pedidoService.SincronizarDetallePagoAsync(id));

            if (!result.Success || result.Data == null)
            {
                _logger.LogError(result.Message);
                return RedirectToAction(nameof(Index));
            }

            var paypalDetail = result.Data;
            var ultimoCapture = paypalDetail.PayPalPaymentCaptures
                .OrderByDescending(c => c.Id)
                .FirstOrDefault();
            var ultimaInfoEnvio = paypalDetail.PayPalPaymentShippings.OrderByDescending(c => c.Id).FirstOrDefault();
            var viewModel = new PayPalPaymentDetailViewModel
            {
                //Datos del pagador
                Id = paypalDetail.Id,
                Intent = paypalDetail.Intent,
                Status = paypalDetail.OrderStatus,
                CreateTime = paypalDetail.CreateTime,
                UpdateTime = paypalDetail.UpdateTime,

                PayerEmail = paypalDetail.PayerEmail,
                PayerFirstName = paypalDetail.PayerFirstName,
                PayerLastName = paypalDetail.PayerLastName,
                PayerId = paypalDetail.PayerId,
                //Datos de envio
                ShippingRecipientName = ultimaInfoEnvio.RecipientName,
                ShippingLine1 = ultimaInfoEnvio.AddressLine1,
                ShippingCity = ultimaInfoEnvio.City,
                ShippingState = ultimaInfoEnvio.State,
                ShippingPostalCode = ultimaInfoEnvio.PostalCode,
                ShippingCountryCode = ultimaInfoEnvio.CountryCode,
                //Importe 
                AmountTotal = paypalDetail.AmountTotal,
                AmountCurrency = paypalDetail.AmountCurrency,
                AmountItemTotal = paypalDetail.AmountItemTotal,
                AmountShipping = paypalDetail.AmountShipping,
                //Datos vendedor
                PayeeMerchantId = paypalDetail.PayeeMerchantId,
                PayeeEmail = paypalDetail.PayeeEmail,
                Description = paypalDetail.Description,
                //Datos del pago
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
