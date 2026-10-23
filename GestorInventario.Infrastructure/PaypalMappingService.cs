using System.Globalization;
using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Infrastructure.ExternalServices;
using GestorInventario.Shared.Utilities;
using Newtonsoft.Json;

namespace GestorInventario.Infrastructure;

public static class PaypalMappingService
{
    public static PayPalOrderDetailsDto MapToOrderDetailsDto(OrderDetailsResponse src)
{
    if (src.PurchaseUnits == null || !src.PurchaseUnits.Any())
        throw new InvalidOperationException("La orden no contiene unidades de compra.");

    var unit = src.PurchaseUnits.First();

    var dto = new PayPalOrderDetailsDto
    {
        OrderId = src.Id,
        Intent = src.Intent,
        Status = src.Status,

        PayerEmail = src.Payer?.Email,
        PayerFirstName = src.Payer?.Name?.GivenName,
        PayerLastName = src.Payer?.Name?.Surname,
        PayerId = src.Payer?.PayerId,

        Description = unit.Description,
        PayeeMerchantId = unit.Payee?.MerchantId,
        PayeeEmail = unit.Payee?.EmailAddress,

        AmountTotal = ConversionExtensions.ToDecimalSafe(unit.Amount?.Value),
        AmountCurrency = unit.Amount?.CurrencyCode,
        AmountItemTotal = ConversionExtensions.ToDecimalSafe(unit.Amount?.Breakdown?.ItemTotal?.Value),
        AmountShipping = ConversionExtensions.ToDecimalSafe(unit.Amount?.Breakdown?.Shipping?.Value ?? "0"),
        AmountTaxTotal = ConversionExtensions.ToDecimalSafe(unit.Amount?.Breakdown?.TaxTotal?.Value ?? "0"),

        Shipping = unit.Shipping == null ? null : new PayPalShippingDto
        {
            RecipientName = unit.Shipping.Name?.FullName,
            AddressLine1 = unit.Shipping.Address?.AddressLine1,
            City = unit.Shipping.Address?.AdminArea2,
            State = unit.Shipping.Address?.AdminArea1,
            PostalCode = unit.Shipping.Address?.PostalCode,
            CountryCode = unit.Shipping.Address?.CountryCode
        },

        Captures = unit.Payments?.Captures?
            .Where(c => c != null)
            .Select(c => MapCapture(c))
            .ToList() ?? new(),

        Refunds = unit.Payments?.Refunds?
            .Where(r => r != null)
            .Select(r => new PayPalRefundDto
            {
                RefundId = r.Id,
                Status = r.Status,
                Amount = ConversionExtensions.ToDecimalSafe(r.Amount?.Value),
                Currency = r.Amount?.CurrencyCode,
                NoteToPayer = r.NoteToPayer,
                InvoiceId = r.InvoiceId,
                CreateTime = ConversionExtensions.ToDateTimeSafe(r.CreateTime),
                UpdateTime = ConversionExtensions.ToDateTimeSafe(r.UpdateTime)
            }).ToList() ?? new(),

        Items = unit.Items?
            .Select(i => new PayPalItemDto
            {
                Name = i.Name,
                Sku = i.Sku,
                UnitAmount = ConversionExtensions.ToDecimalSafe(i.UnitAmount?.Value),
                Tax = ConversionExtensions.ToDecimalSafe(i.Tax?.Value),
                Quantity = ConversionExtensions.ToIntSafe(i.Quantity),
                Currency = i.UnitAmount?.CurrencyCode
            }).ToList() ?? new()
    };

    return dto;
}

private static PayPalCaptureDto MapCapture(CaptureDetails c)
{
    decimal? exchangeRate = null;
    var rateStr = c.SellerReceivableBreakdown?.ExchangeRate?.Value;
    if (!string.IsNullOrEmpty(rateStr) &&
        decimal.TryParse(rateStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
    {
        exchangeRate = rate;
    }

    return new PayPalCaptureDto
    {
        CaptureId = c.Id,
        Status = c.Status,
        Amount = ConversionExtensions.ToDecimalSafe(c.Amount?.Value),
        Currency = c.Amount?.CurrencyCode,
        FinalCapture = c.FinalCapture,
        ProtectionEligibility = c.SellerProtection?.Status,
        TransactionFeeAmount = ConversionExtensions.ToDecimalSafe(c.SellerReceivableBreakdown?.PaypalFee?.Value),
        TransactionFeeCurrency = c.SellerReceivableBreakdown?.PaypalFee?.CurrencyCode,
        ReceivableAmount = ConversionExtensions.ToDecimalSafe(c.SellerReceivableBreakdown?.NetAmount?.Value),
        ReceivableCurrency = c.SellerReceivableBreakdown?.NetAmount?.CurrencyCode,
        ExchangeRate = exchangeRate,
        DisputeCategoriesJson = c.SellerProtection?.DisputeCategories != null
            ? JsonConvert.SerializeObject(c.SellerProtection.DisputeCategories)
            : null,
        CreateTime = ConversionExtensions.ToDateTimeSafe(c.CreateTime),
        UpdateTime = ConversionExtensions.ToDateTimeSafe(c.UpdateTime),
        InvoiceId = c.InvoiceId
    };
}
}