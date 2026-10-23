using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Infrastructure.ExternalServices;

namespace GestorInventario.Infrastructure.Mappers;

public static class PlanDetailsMapper
{
    public static PlanDetailsDto ToDto(PaypalPlanDetailsResponse src)
    {
        return new PlanDetailsDto
        {
            Id = src.Id,
            ProductId = src.ProductId,
            Name = src.Name,
            Description = src.Description,
            Status = src.Status,
            BillingCycles = src.BillingCycles?.Select(c => new PlanBillingCycleDto
            {
                TenureType = c.TenureType,
                Sequence = c.Sequence,
                TotalCycles = c.TotalCycles,
                Frequency = new PlanFrequencyDto
                {
                    IntervalUnit = c.Frequency.IntervalUnit,
                    IntervalCount = c.Frequency.IntervalCount
                },
                PricingScheme = new PlanPricingSchemeDto
                {
                    FixedPrice = c.PricingScheme?.FixedPrice == null ? null : new PlanFixedPriceDto
                    {
                        Value = c.PricingScheme.FixedPrice.Value,
                        CurrencyCode = c.PricingScheme.FixedPrice.CurrencyCode
                    }
                }
            }).ToList() ?? new(),
            PaymentPreferences = src.PaymentPreferences == null ? null : new PlanPaymentPreferencesDto
            {
                AutoBillOutstanding = src.PaymentPreferences.AutoBillOutstanding,
                SetupFeeFailureAction = src.PaymentPreferences.SetupFeeFailureAction,
                PaymentFailureThreshold = src.PaymentPreferences.PaymentFailureThreshold,
                SetupFee = src.PaymentPreferences.SetupFee == null ? null : new PlanFixedPriceDto
                {
                    Value = src.PaymentPreferences.SetupFee.Value,
                    CurrencyCode = src.PaymentPreferences.SetupFee.CurrencyCode
                }
            },
            Taxes = src.Taxes == null ? null : new PlanTaxesDto
            {
                Percentage = src.Taxes.Percentage,
                Inclusive = src.Taxes.Inclusive
            }
        };
    }
}