using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GestorInventario.Infraestructure.Repositories
{
    public class PaypalRepository:IPaypalRepository
    {
        public readonly GestorInventarioContext _context;
        private readonly IPaypalService _paypalService;
        private readonly ILogger<PaypalRepository> _logger;
        public PaypalRepository(GestorInventarioContext context, IPaypalService service, ILogger<PaypalRepository> logger)
        {
            _context = context;
            _paypalService = service;
            _logger = logger;
        }

        public async Task<List<SubscriptionDetail>> ObtenerSuscriptcionesActivas(string planId) => await _context.SubscriptionDetails.Where(s => s.PlanId == planId && s.Status != "CANCELLED").ToListAsync();
        public async Task<List<UserSubscription>> SusbcripcionesUsuario(string planId)=> await _context.UserSubscriptions.Where(us => us.PaypalPlanId == planId).ToListAsync();
        public async Task<SubscriptionDetail> ObtenerSubscripcion(string subscription_id) => await _context.SubscriptionDetails.FirstOrDefaultAsync(x => x.SubscriptionId == subscription_id);
        public async Task DetallesSubscripcion(string id) {

            try
            {
                var existingPlan = await _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == id);
                if (existingPlan != null)
                {
                    return;
                }
                var planRequest = await _paypalService.ObtenerDetallesPlan(id);
                var planDetails = new PlanDetail
                {
                    Id = Guid.NewGuid().ToString(),
                    PaypalPlanId = planRequest.Id,
                    ProductId = planRequest.ProductId,
                    Name = planRequest.Name,
                    Description = planRequest.Description,
                    Status = planRequest.Status,
                    AutoBillOutstanding = planRequest.PaymentPreferences.AutoBillOutstanding,
                    SetupFee = planRequest.PaymentPreferences.SetupFee?.Value != null ?
                decimal.Parse(planRequest.PaymentPreferences.SetupFee.Value.ToString(), CultureInfo.InvariantCulture) : 0,
                    SetupFeeFailureAction = planRequest.PaymentPreferences.SetupFeeFailureAction,
                    PaymentFailureThreshold = planRequest.PaymentPreferences.PaymentFailureThreshold,
                    TaxPercentage = planRequest.Taxes?.Percentage != null ?
                     decimal.Parse(planRequest.Taxes.Percentage.ToString(), CultureInfo.InvariantCulture) : 0,
                    TaxInclusive = planRequest.Taxes?.Inclusive ?? false
                };



                // Verificar si existe un ciclo de facturación de prueba si esta condicion se cumple es que existe dias de prueba.
                if (planRequest.BillingCycles.Count > 1)
                {
                    planDetails.TrialIntervalUnit = planRequest.BillingCycles[0].Frequency.IntervalUnit;
                    planDetails.TrialIntervalCount = planRequest.BillingCycles[0].Frequency.IntervalCount;
                    planDetails.TrialTotalCycles = planRequest.BillingCycles[0].TotalCycles;

                    // Convertir TrialFixedPrice a decimal si no es nulo
                    planDetails.TrialFixedPrice = planRequest.BillingCycles[0].PricingScheme.FixedPrice?.Value != null ?
                                                  decimal.Parse(planRequest.BillingCycles[0].PricingScheme.FixedPrice.Value.ToString(), CultureInfo.InvariantCulture) : 0;

                    // Información del ciclo regular
                    planDetails.RegularIntervalUnit = planRequest.BillingCycles[1].Frequency.IntervalUnit;
                    planDetails.RegularIntervalCount = planRequest.BillingCycles[1].Frequency.IntervalCount;
                    planDetails.RegularTotalCycles = planRequest.BillingCycles[1].TotalCycles;

                    // Convertir RegularFixedPrice a decimal si no es nulo
                    planDetails.RegularFixedPrice = planRequest.BillingCycles[1].PricingScheme.FixedPrice?.Value != null ?
                                                    decimal.Parse(planRequest.BillingCycles[1].PricingScheme.FixedPrice.Value.ToString(), CultureInfo.InvariantCulture) : 0;
                }

                else if (planRequest.BillingCycles.Count == 1)
                {
                    // Solo hay ciclo regular
                    planDetails.RegularIntervalUnit = planRequest.BillingCycles[0].Frequency.IntervalUnit;
                    planDetails.RegularIntervalCount = planRequest.BillingCycles[0].Frequency.IntervalCount;
                    planDetails.RegularTotalCycles = planRequest.BillingCycles[0].TotalCycles;
                    planDetails.RegularFixedPrice = decimal.Parse(planRequest.BillingCycles[0].PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture);
                }

                _context.PlanDetails.Add(planDetails);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al guardar los detalles del plan en la base de datos: {ex.Message}", ex);

            }
        }
        public async Task SavePlanDetailsToDatabase(string createdPlanId, dynamic planRequest)
        {
            var planDetails = new PlanDetail
            {
                Id = Guid.NewGuid().ToString(),
                PaypalPlanId = createdPlanId,
                ProductId = planRequest.product_id,
                Name = planRequest.name,
                Description = planRequest.description,
                Status = planRequest.status,
                AutoBillOutstanding = planRequest.payment_preferences.auto_bill_outstanding,
                SetupFee = decimal.Parse(planRequest.payment_preferences.setup_fee.value, CultureInfo.InvariantCulture),
                SetupFeeFailureAction = planRequest.payment_preferences.setup_fee_failure_action,
                PaymentFailureThreshold = planRequest.payment_preferences.payment_failure_threshold,
                TaxPercentage = decimal.Parse(planRequest.taxes.percentage, CultureInfo.InvariantCulture),
                TaxInclusive = planRequest.taxes.inclusive
            };

            // Verificar si existe un ciclo de facturación de prueba
            if (planRequest.billing_cycles.Count > 1)
            {
                planDetails.TrialIntervalUnit = planRequest.billing_cycles[0].frequency.interval_unit;
                planDetails.TrialIntervalCount = planRequest.billing_cycles[0].frequency.interval_count;
                planDetails.TrialTotalCycles = planRequest.billing_cycles[0].total_cycles;
                planDetails.TrialFixedPrice = decimal.Parse(planRequest.billing_cycles[0].pricing_scheme.fixed_price.value, CultureInfo.InvariantCulture);

                // Información del ciclo regular
                planDetails.RegularIntervalUnit = planRequest.billing_cycles[1].frequency.interval_unit;
                planDetails.RegularIntervalCount = planRequest.billing_cycles[1].frequency.interval_count;
                planDetails.RegularTotalCycles = planRequest.billing_cycles[1].total_cycles;
                planDetails.RegularFixedPrice = decimal.Parse(planRequest.billing_cycles[1].pricing_scheme.fixed_price.value, CultureInfo.InvariantCulture);
            }
            else if (planRequest.billing_cycles.Count == 1)
            {
                // Solo hay ciclo regular
                planDetails.RegularIntervalUnit = planRequest.billing_cycles[0].frequency.interval_unit;
                planDetails.RegularIntervalCount = planRequest.billing_cycles[0].frequency.interval_count;
                planDetails.RegularTotalCycles = planRequest.billing_cycles[0].total_cycles;
                planDetails.RegularFixedPrice = decimal.Parse(planRequest.billing_cycles[0].pricing_scheme.fixed_price.value, CultureInfo.InvariantCulture);
            }

            _context.PlanDetails.Add(planDetails);
            await _context.SaveChangesAsync();
        }

    }
}
