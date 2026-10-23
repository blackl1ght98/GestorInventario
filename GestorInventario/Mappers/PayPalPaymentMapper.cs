using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Domain.Models;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Mappers;

public static class PayPalPaymentMapper
{
    public static PayPalPaymentDetailViewModel ToViewModel(PayPalPaymentDetail entity)
    {
        if (entity == null) return null;

        var ultimoCapture = entity.PayPalPaymentCaptures?.OrderByDescending(c => c.Id).FirstOrDefault();
        var ultimaInfoEnvio = entity.PayPalPaymentShippings?.OrderByDescending(c => c.Id).FirstOrDefault();
        var ultimoRefund = entity.PayPalPaymentRefunds?.OrderByDescending(r => r.CreateTime).FirstOrDefault();
        var totalReembolsado = entity.PayPalPaymentRefunds?.Sum(r => r.Amount) ?? 0m;

        return new PayPalPaymentDetailViewModel
        {
            Id = entity.Id,
            Intent = entity.Intent,
            Status = entity.OrderStatus,
            CreateTime = entity.CreateTime,
            UpdateTime = entity.UpdateTime,
            Payer = new PayerInfo {
                Email = entity.PayerEmail, FirstName = entity.PayerFirstName,
                LastName = entity.PayerLastName, PayerId = entity.PayerId
            },
            Shipping = new ShippingInfo {
                RecipientName = ultimaInfoEnvio?.RecipientName ?? string.Empty,
                Line1 = ultimaInfoEnvio?.AddressLine1 ?? string.Empty,
                City = ultimaInfoEnvio?.City ?? string.Empty,
                State = ultimaInfoEnvio?.State ?? string.Empty,
                PostalCode = ultimaInfoEnvio?.PostalCode ?? string.Empty,
                CountryCode = ultimaInfoEnvio?.CountryCode ?? string.Empty,
            },
            TotalReembolsado = totalReembolsado,
            Amount = new AmountInfo {
                Total = entity.AmountTotal,
                Currency = entity.AmountCurrency ?? string.Empty,
                ItemTotal = entity.AmountItemTotal,
                Shipping = entity.AmountShipping,
                Tax = entity.AmountTax,
            },
            Payee = new PayeeInfo {
                MerchantId = entity.PayeeMerchantId ?? string.Empty,
                Email = entity.PayeeEmail ?? string.Empty,
                Description = entity.Description ?? string.Empty,
            },
            Capture = ultimoCapture == null ? null : new CaptureInfo {
                SaleId = ultimoCapture.CaptureId ?? string.Empty,
                Status = ultimoCapture.Status ?? string.Empty,
                Amount = ultimoCapture.Amount ,
                Currency = ultimoCapture.Currency ?? string.Empty,
                ProtectionEligibility = ultimoCapture.ProtectionEligibility ?? string.Empty,
                TransactionFeeAmount = ultimoCapture.TransactionFeeAmount ,
                TransactionFeeCurrency = ultimoCapture.TransactionFeeCurrency ?? string.Empty,
                ReceivableAmount = ultimoCapture.ReceivableAmount ,
                ReceivableCurrency = ultimoCapture.ReceivableCurrency ?? string.Empty,
                ExchangeRate = ultimoCapture.ExchangeRate ,
                FinalCapture = ultimoCapture.FinalCapture ,
                DisputeCategories = ultimoCapture.DisputeCategories ?? string.Empty,
            },
            Refund = ultimoRefund == null ? null : new RefundInfo {
                RefundId = ultimoRefund.RefundId,
                Status = ultimoRefund.Status,
                Amount = ultimoRefund.Amount,
                Currency = ultimoRefund.Currency,
                NoteToPayer = ultimoRefund.NoteToPayer,
                CreateTime = ultimoRefund.CreateTime,
                UpdateTime = ultimoRefund.UpdateTime,
            },
            Items = entity.PayPalPaymentItems?.Select(item => new PayPalPaymentItemDto {
                ItemName = item.ItemName,
                ItemSku = item.ItemSku,
                ItemPrice = item.ItemPrice,
                ItemCurrency = item.ItemCurrency,
                ItemTax = item.ItemTax,
                ItemQuantity = item.ItemQuantity,
            }).ToList() ?? new List<PayPalPaymentItemDto>(),
        };
    }
}