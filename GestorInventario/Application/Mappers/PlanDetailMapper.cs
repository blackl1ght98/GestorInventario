using GestorInventario.Application.DTOS.Paypal.Projections;
using GestorInventario.Application.DTOS.Paypal.Responses.GET.Subscription;
using GestorInventario.Domain.Models;
using System.Globalization;

namespace GestorInventario.Application.Mappers
{
    public static class PlanDetailMapper
    {
        public static PlanProjection ToPlanProjection(this PlanDetail detail)
        {
            var billingCycles = new List<BillingCycle>();

            // Ciclo de prueba (si existe)
            if (detail.TrialFixedPrice.HasValue && detail.TrialIntervalUnit != null)
            {
                billingCycles.Add(new BillingCycle
                {
                    TenureType = "TRIAL",
                    TotalCycles = detail.TrialTotalCycles ?? 0,
                    Sequence = 1,
                    PricingScheme = new PricingScheme
                    {
                        FixedPrice = new Money
                        {
                            Value = detail.TrialFixedPrice.Value.ToString("F2", CultureInfo.InvariantCulture),
                            CurrencyCode = detail.CurrencyCode ?? "EUR"
                        }
                    },
                    Frequency = new Frequency
                    {
                        IntervalUnit = detail.TrialIntervalUnit,
                        IntervalCount = detail.TrialIntervalCount ?? 1
                    }
                });
            }

            // Ciclo regular (siempre debería existir a menos que la api de paypal cambie)
            if (detail.RegularFixedPrice.HasValue && detail.RegularIntervalUnit != null)
            {
                billingCycles.Add(new BillingCycle
                {
                    TenureType = "REGULAR",
                    TotalCycles = detail.RegularTotalCycles ?? 0,
                    Sequence = detail.TrialFixedPrice.HasValue ? 2 : 1,
                    PricingScheme = new PricingScheme
                    {
                        FixedPrice = new Money
                        {
                            Value = detail.RegularFixedPrice.Value.ToString("F2", CultureInfo.InvariantCulture),
                            CurrencyCode = detail.CurrencyCode ?? "EUR"
                        }
                    },
                    Frequency = new Frequency
                    {
                        IntervalUnit = detail.RegularIntervalUnit,
                        IntervalCount = detail.RegularIntervalCount ?? 1
                    }
                });
            }

            return new PlanProjection
            {
                Id = detail.PaypalPlanId,
                productId = detail.ProductId ?? string.Empty,
                Name = detail.Name ?? "Sin nombre",
                Description = detail.Description ?? string.Empty,
                Status = detail.Status ?? "DESCONOCIDO",
                Usage_type = "LICENSED", 
                CreateTime = DateTime.UtcNow, 
                Billing_cycles = billingCycles,
                Taxes = new Taxes
                {
                    Percentage = (detail.TaxPercentage ?? 0).ToString("F2", CultureInfo.InvariantCulture),
                    Inclusive = detail.TaxInclusive ?? false
                },
                CurrencyCode = detail.CurrencyCode ?? "EUR"
            };
        }
    }
}
