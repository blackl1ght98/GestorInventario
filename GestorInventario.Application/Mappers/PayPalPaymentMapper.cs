using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Order;
using GestorInventario.Shared.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GestorInventario.Application.Mappers
{
    public static class PayPalPaymentMapper
    {
        /// <summary>
        /// Mapea un único capture de PayPal al modelo de dominio.
        /// </summary>
        public static PayPalPaymentCapture MapearCapture(
            CaptureDetails capture, string paymentId, int pedidoId)
        {
            var breakdown = capture.SellerReceivableBreakdown;

            var result = new PayPalPaymentCapture
            {
                PaymentId = paymentId,
                CaptureId = capture.Id,
                Status = capture.Status,
                PedidoId = pedidoId,
                Amount = ConversionExtensions.ToDecimalSafe(capture.Amount.Value),
                Currency = capture.Amount.CurrencyCode,
                ProtectionEligibility = capture.SellerProtection.Status,
                TransactionFeeAmount = ConversionExtensions.ToDecimalSafe(breakdown.PaypalFee.Value),
                TransactionFeeCurrency = breakdown.PaypalFee.CurrencyCode,
                ReceivableAmount = ConversionExtensions.ToDecimalSafe(breakdown.NetAmount.Value),
                ReceivableCurrency = breakdown.NetAmount.CurrencyCode,
                FinalCapture = capture.FinalCapture,
                CreateTime = ConversionExtensions.ToDateTimeSafe(capture.CreateTime),
                UpdateTime = ConversionExtensions.ToDateTimeSafe(capture.UpdateTime),
                ExchangeRate = ParseExchangeRate(breakdown.ExchangeRate?.Value),
                DisputeCategories = capture.SellerProtection.DisputeCategories != null
                    ? JsonConvert.SerializeObject(capture.SellerProtection.DisputeCategories)
                    : null!
            };

            return result;
        }
        /// <summary>
        /// Mapea un único refund de PayPal al modelo de dominio.
        /// </summary>
        public static PayPalPaymentRefund MapearRefund(
            RefundDetails refund, string paymentId, int pedidoId)
        {
            var breakdown = refund.SellerPayableBreakdown;

            return new PayPalPaymentRefund
            {
                PaymentId = paymentId,
                RefundId = refund.Id,
                PedidoId = pedidoId,
                Status = refund.Status,
                Amount = ConversionExtensions.ToDecimalSafe(refund.Amount.Value),
                Currency = refund.Amount.CurrencyCode,
                NoteToPayer = refund.NoteToPayer,
                TotalRefundedAmount = breakdown != null
                    ? ConversionExtensions.ToDecimalSafe(breakdown.TotalRefundedAmount?.Value)
                    : null,
                PaypalFee = breakdown != null
                    ? ConversionExtensions.ToDecimalSafe(breakdown.PaypalFee?.Value)
                    : null,
                NetAmount = breakdown != null
                    ? ConversionExtensions.ToDecimalSafe(breakdown.NetAmount?.Value)
                    : null,
                CreateTime = ConversionExtensions.ToDateTimeSafe(refund.CreateTime),
                UpdateTime = ConversionExtensions.ToDateTimeSafe(refund.UpdateTime),
            };
        }
        /// <summary>
        /// Mapea los datos del pagador (payer) desde la respuesta de PayPal.
        /// </summary>
        public static void MapearPayer(
            OrderDetailsResponse fuente, PayPalPaymentDetail destino)
        {
            destino.Intent = fuente.Intent;
            destino.OrderStatus = fuente.Status;
            destino.PayerEmail = fuente.Payer.Email;
            destino.PayerFirstName = fuente.Payer.Name.GivenName;
            destino.PayerLastName = fuente.Payer.Name.Surname;
            destino.PayerId = fuente.Payer.PayerId;
        }
        /// <summary>
        /// Mapea los montos principales. Si el item total es 0, lo calcula desde los items.
        /// </summary>
        public static void MapearMontos(
            PurchaseUnitDetails unidad, PayPalPaymentDetail detallePago)
        {
            detallePago.AmountTotal = ConversionExtensions.ToDecimalSafe(unidad.Amount.Value);
            detallePago.AmountCurrency = unidad.Amount.CurrencyCode;
            detallePago.AmountItemTotal = ConversionExtensions.ToDecimalSafe(
                unidad.Amount.Breakdown.ItemTotal.Value);

            if (detallePago.AmountItemTotal == 0 && unidad.Items != null)
            {
                detallePago.AmountItemTotal = unidad.Items.Sum(item =>
                    ConversionExtensions.ToDecimalSafe(item.UnitAmount.Value) *
                    ConversionExtensions.ToIntSafe(item.Quantity));
            }

            detallePago.AmountShipping = ConversionExtensions.ToDecimalSafe(
                unidad.Amount.Breakdown.Shipping.Value);
            detallePago.PayeeMerchantId = unidad.Payee.MerchantId;
            detallePago.PayeeEmail = unidad.Payee.EmailAddress;
            detallePago.Description = unidad.Description;
            detallePago.AmountTax = ConversionExtensions.ToDecimalSafe(
                unidad.Amount.Breakdown.TaxTotal.Value);
        }
        /// <summary>
        /// Parsea el tipo de cambio. Retorna 0 si es nulo o inválido.
        /// </summary>
        private static decimal ParseExchangeRate(string? exchangeValue)
        {
            if (string.IsNullOrEmpty(exchangeValue))
                return 0;

            if (decimal.TryParse(exchangeValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                return rate;

            return 0;
        }

    }
}
