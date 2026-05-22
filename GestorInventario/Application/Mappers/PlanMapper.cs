using GestorInventario.Application.DTOS.Paypal.Projections;
using GestorInventario.Application.DTOS.Paypal.Responses.GET.Subscription;
using GestorInventario.Application.DTOS.Paypal.Responses.POST.Subscription;
using GestorInventario.Domain.Models;
using System.Globalization;

namespace GestorInventario.Application.Mappers
{
    public static class PlanMapper
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
            if (detail.RegularFixedPrice > 0 && detail.RegularIntervalUnit != null)
            {
                billingCycles.Add(new BillingCycle
                {
                    TenureType = "REGULAR",
                    TotalCycles = detail.RegularTotalCycles,
                    Sequence = detail.TrialFixedPrice.HasValue ? 2 : 1,
                    PricingScheme = new PricingScheme
                    {
                        FixedPrice = new Money
                        {
                            Value = detail.RegularFixedPrice.ToString("F2", CultureInfo.InvariantCulture),
                            CurrencyCode = detail.CurrencyCode ?? "EUR"
                        }
                    },
                    Frequency = new Frequency
                    {
                        IntervalUnit = detail.RegularIntervalUnit,
                        IntervalCount = detail.RegularIntervalCount 
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
        public static PlanDetail MapPayPalPlanToEntity(string planId, PaypalPlanDetailsDto planDetails)
        {
            var planDetail = new PlanDetail
            {
                Id = Guid.NewGuid().ToString(),
                PaypalPlanId = planId,
                ProductId = planDetails.ProductId,
                Name = planDetails.Name,
                Description = planDetails.Description,
                Status = planDetails.Status,


                AutoBillOutstanding = planDetails.PaymentPreferences?.AutoBillOutstanding ?? true,

                SetupFee = planDetails.PaymentPreferences?.SetupFee?.Value != null
          ? decimal.Parse(planDetails.PaymentPreferences.SetupFee.Value, CultureInfo.InvariantCulture)
          : 0,


                SetupFeeFailureAction = planDetails.PaymentPreferences?.SetupFeeFailureAction ?? "CONTINUE",


                PaymentFailureThreshold = planDetails.PaymentPreferences?.PaymentFailureThreshold ?? 3,

                TaxPercentage = planDetails.Taxes?.Percentage != null
          ? decimal.Parse(planDetails.Taxes.Percentage, CultureInfo.InvariantCulture)
          : 0,

                TaxInclusive = planDetails.Taxes?.Inclusive ?? false
            };

            // Manejo de ciclos de facturación
            if (planDetails.BillingCycles.Length > 1)
            {
                planDetail.TrialIntervalUnit = planDetails.BillingCycles[0].Frequency.IntervalUnit;
                planDetail.TrialIntervalCount = planDetails.BillingCycles[0].Frequency.IntervalCount;
                planDetail.TrialTotalCycles = planDetails.BillingCycles[0].TotalCycles;
                planDetail.TrialFixedPrice = planDetails.BillingCycles[0].PricingScheme.FixedPrice?.Value != null
                    ? decimal.Parse(planDetails.BillingCycles[0].PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture)
                    : 0;
                planDetail.CurrencyCode = planDetails.BillingCycles?.FirstOrDefault()?.PricingScheme?.FixedPrice?.CurrencyCode;
                planDetail.RegularIntervalUnit = planDetails.BillingCycles[1].Frequency.IntervalUnit;
                planDetail.RegularIntervalCount = planDetails.BillingCycles[1].Frequency.IntervalCount;
                planDetail.RegularTotalCycles = planDetails.BillingCycles[1].TotalCycles;
                planDetail.RegularFixedPrice = planDetails.BillingCycles[1].PricingScheme.FixedPrice?.Value != null
                    ? decimal.Parse(planDetails.BillingCycles[1].PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture)
                    : 0;
            }
            else if (planDetails.BillingCycles.Length == 1)
            {
                planDetail.RegularIntervalUnit = planDetails.BillingCycles[0].Frequency.IntervalUnit;
                planDetail.RegularIntervalCount = planDetails.BillingCycles[0].Frequency.IntervalCount;
                planDetail.RegularTotalCycles = planDetails.BillingCycles[0].TotalCycles;
                planDetail.RegularFixedPrice = planDetails.BillingCycles[0].PricingScheme.FixedPrice?.Value != null
                    ? decimal.Parse(planDetails.BillingCycles[0].PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture)
                    : 0;
            }

            return planDetail;
        }
    }
}
