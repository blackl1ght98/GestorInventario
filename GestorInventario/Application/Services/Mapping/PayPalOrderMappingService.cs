using GestorInventario.Application.DTOs.Paypal.Responses.GET.Order;
using GestorInventario.Domain.Models;

using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using Newtonsoft.Json;
using System.Globalization;

namespace GestorInventario.Application.Services.Mapping
{
    public class PayPalOrderMappingService: IPayPalOrderMappingService
    {
        private readonly IConversionUtils _conversion;

        public PayPalOrderMappingService(IConversionUtils conversion)
        {
            _conversion = conversion;
        }
        public PayPalPaymentDetail MapearOrdenADetallePago(OrderDetailsResponse detallespago)
        {
            if (detallespago.PurchaseUnits == null || !detallespago.PurchaseUnits.Any())
            {
               
                throw new InvalidOperationException("No se encuentran las unidades de pago en la petición");
            }

            var firstPurchaseUnit = detallespago.PurchaseUnits.First();

            var detallesPagoRembolso = new PayPalPaymentDetail
            {
                Id = detallespago.Id,
                Intent = detallespago.Intent,
                OrderStatus = detallespago.Status,

                PayerEmail = detallespago.Payer?.Email,
                PayerFirstName = detallespago.Payer?.Name?.GivenName,
                PayerLastName = detallespago.Payer?.Name?.Surname,
                PayerId = detallespago.Payer?.PayerId,

                PayPalPaymentShippings = new List<PayPalPaymentShipping>(),
                PayPalPaymentCaptures = new List<PayPalPaymentCapture>()
            };

            // ----------------------------
            // ENVÍO
            // ----------------------------
            var informacionEnvio = new PayPalPaymentShipping
            {
                PaymentId = detallespago.Id,
                RecipientName = firstPurchaseUnit?.Shipping?.Name?.FullName,
                AddressLine1 = firstPurchaseUnit?.Shipping?.Address?.AddressLine1,
                City = firstPurchaseUnit?.Shipping?.Address?.AdminArea2,
                State = firstPurchaseUnit?.Shipping?.Address?.AdminArea1,
                PostalCode = firstPurchaseUnit?.Shipping?.Address?.PostalCode,
                CountryCode = firstPurchaseUnit?.Shipping?.Address?.CountryCode
            };

            detallesPagoRembolso.PayPalPaymentShippings.Add(informacionEnvio);

            // ----------------------------
            // IMPORTES
            // ----------------------------
            if (firstPurchaseUnit.Amount != null)
            {
                detallesPagoRembolso.AmountTotal =
                    (decimal)_conversion.ConvertToDecimal(firstPurchaseUnit.Amount.Value ?? "0");

                detallesPagoRembolso.AmountCurrency =
                    firstPurchaseUnit.Amount.CurrencyCode;

                if (firstPurchaseUnit.Amount.Breakdown != null)
                {
                    detallesPagoRembolso.AmountItemTotal =
                       (decimal) _conversion.ConvertToDecimal(
                            firstPurchaseUnit.Amount.Breakdown.ItemTotal.Value);

                    detallesPagoRembolso.AmountShipping =
                       (decimal) _conversion.ConvertToDecimal(
                            firstPurchaseUnit.Amount.Breakdown.Shipping.Value?? "0");
                }
            }

            // ----------------------------
            // PAYEE
            // ----------------------------
            if (firstPurchaseUnit?.Payee != null)
            {
                detallesPagoRembolso.PayeeMerchantId =
                    firstPurchaseUnit.Payee.MerchantId;

                detallesPagoRembolso.PayeeEmail =
                    firstPurchaseUnit.Payee.EmailAddress;
            }

            detallesPagoRembolso.Description =
                firstPurchaseUnit?.Description;

            // ----------------------------
            // CAPTURAS
            // ----------------------------
            if (firstPurchaseUnit?.Payments?.Captures != null &&
                firstPurchaseUnit.Payments.Captures.Any())
            {
                foreach (var capture in firstPurchaseUnit.Payments.Captures)
                {
                    if (capture == null)
                        continue;

                    var nuevaCaptura = new PayPalPaymentCapture
                    {
                        PaymentId = detallespago.Id,

                        CaptureId = capture.Id,
                        Status = capture.Status,

                        Amount = (capture.Amount != null
                            ? _conversion.ConvertToDecimal(capture.Amount.Value)
                            : 0),

                        Currency = capture.Amount?.CurrencyCode,

                        ProtectionEligibility =
                            capture.SellerProtection?.Status,

                        TransactionFeeAmount =
                            (capture.SellerReceivableBreakdown?.PaypalFee != null
                                ? _conversion.ConvertToDecimal(
                                    capture.SellerReceivableBreakdown.PaypalFee.Value)
                                : 0),

                        TransactionFeeCurrency =
                            capture.SellerReceivableBreakdown?.PaypalFee?.CurrencyCode,

                        ReceivableAmount =
                            (capture.SellerReceivableBreakdown?.NetAmount != null
                                ? _conversion.ConvertToDecimal(
                                    capture.SellerReceivableBreakdown.NetAmount.Value)
                                : 0),

                        ReceivableCurrency =
                            capture.SellerReceivableBreakdown?.NetAmount?.CurrencyCode,

                        FinalCapture = capture.FinalCapture,

                        CreateTime =
                            _conversion.ConvertToDateTime(capture.CreateTime),

                        UpdateTime =
                            _conversion.ConvertToDateTime(capture.UpdateTime)
                    };

                    // ExchangeRate
                    var exchangeRateValue =
                        capture.SellerReceivableBreakdown?.ExchangeRate?.Value;

                    if (!string.IsNullOrEmpty(exchangeRateValue) &&
                        decimal.TryParse(
                            exchangeRateValue,
                            NumberStyles.Any,
                            CultureInfo.InvariantCulture,
                            out decimal exchangeRate))
                    {
                        nuevaCaptura.ExchangeRate = exchangeRate;
                    }

                    // Dispute Categories
                    if (capture.SellerProtection?.DisputeCategories != null)
                    {
                        nuevaCaptura.DisputeCategories =
                            JsonConvert.SerializeObject(
                                capture.SellerProtection.DisputeCategories);
                    }

                    detallesPagoRembolso.PayPalPaymentCaptures.Add(nuevaCaptura);
                }
            }

            return detallesPagoRembolso;
        }
    }
}
