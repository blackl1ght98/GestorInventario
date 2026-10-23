using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Infrastructure.ExternalServices;

namespace GestorInventario.Infrastructure.Mappers;

public static class PlanPageMapper
{
    public static SubscriptionPlan ToDto(PaypalPlanResponse src)
    {
        return new SubscriptionPlan
        {
            Id = src.Id,
            ProductId = src.ProductId,
            Name = src.Name,
            Status = src.Status,
            Description = src.Description,
            QuantitySupported = src.QuantitySupported,
            CreateTime = src.CreateTime,
            UpdateTime = src.UpdateTime,

            BillingCycles = src.BillingCycles?
                .Select(c => new CatalogPlanBillingCycleDto
                {
                    TenureType = c.TenureType,
                    Sequence = c.Sequence,
                    TotalCycles = c.TotalCycles,
                    Frequency = new CatalogPlanFrequencyDto
                    {
                        IntervalUnit = c.Frequency.IntervalUnit,
                        IntervalCount = c.Frequency.IntervalCount
                    },
                    PricingScheme = new CatalogPlanPricingSchemeDto
                    {
                        Version = c.PricingScheme.Version,
                        CreateTime = c.PricingScheme.CreateTime,
                        UpdateTime = c.PricingScheme.UpdateTime,
                        FixedPrice = new PlanMoneyDto
                        {
                            CurrencyCode = c.PricingScheme.FixedPrice.CurrencyCode,
                            Value = c.PricingScheme.FixedPrice.Value
                        }
                    }
                })
                .ToList()
                ?? new List<CatalogPlanBillingCycleDto>(),

            PaymentPreferences = src.PaymentPreferences == null
                ? null
                : new CatalogPlanPaymentPreferencesDto
                {
                    ServiceType = src.PaymentPreferences.ServiceType,
                    AutoBillOutstanding = src.PaymentPreferences.AutoBillOutstanding,
                    SetupFeeFailureAction = src.PaymentPreferences.SetupFeeFailureAction,
                    PaymentFailureThreshold = src.PaymentPreferences.PaymentFailureThreshold,
                    SetupFee = src.PaymentPreferences.SetupFee == null
                        ? null
                        : new PlanMoneyDto
                        {
                            CurrencyCode = src.PaymentPreferences.SetupFee.CurrencyCode,
                            Value = src.PaymentPreferences.SetupFee.Value
                        }
                },

            Taxes = src.Taxes == null
                ? null
                : new CatalogPlanTaxesDto
                {
                    Percentage = src.Taxes.Percentage,
                    Inclusive = src.Taxes.Inclusive
                }
        };
    }

    public static List<SubscriptionPlan> ToDtoList(IEnumerable<PaypalPlanResponse> plans)
        => plans.Select(ToDto).ToList();

    public static SubscriptionPlansPageDto ToPageDto(
        IEnumerable<PaypalPlanResponse> plans,
        bool hasNextPage)
    {
        return new SubscriptionPlansPageDto
        {
            Plans = ToDtoList(plans),
            HasNextPage = hasNextPage
        };
    }
}