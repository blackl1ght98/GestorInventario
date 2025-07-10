using GestorInventario.Application.DTOs;
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
      
        public async Task SavePlanDetailsAsync(string planId, PaypalPlanDetailsDto planDetails)
        {
            try
            {
                // Verificar si el plan ya existe
                var existingPlan = await _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId);
                if (existingPlan != null)
                {
                    _logger.LogInformation($"El plan con ID {planId} ya existe en la base de datos.");
                    return;
                }

                var planDetail = new PlanDetail
                {
                    Id = Guid.NewGuid().ToString(),
                    PaypalPlanId = planId,
                    ProductId = planDetails.ProductId,
                    Name = planDetails.Name,
                    Description = planDetails.Description,
                    Status = planDetails.Status,
                    AutoBillOutstanding = planDetails.PaymentPreferences.AutoBillOutstanding,
                    SetupFee = planDetails.PaymentPreferences.SetupFee?.Value != null
                        ? decimal.Parse(planDetails.PaymentPreferences.SetupFee.Value, CultureInfo.InvariantCulture)
                        : 0,
                    SetupFeeFailureAction = planDetails.PaymentPreferences.SetupFeeFailureAction,
                    PaymentFailureThreshold = planDetails.PaymentPreferences.PaymentFailureThreshold,
                    TaxPercentage = planDetails.Taxes?.Percentage != null
                        ? decimal.Parse(planDetails.Taxes.Percentage, CultureInfo.InvariantCulture)
                        : 0,
                    TaxInclusive = planDetails.Taxes?.Inclusive ?? false
                };

                // Manejar ciclos de facturación
                if (planDetails.BillingCycles.Length > 1)
                {
                    planDetail.TrialIntervalUnit = planDetails.BillingCycles[0].Frequency.IntervalUnit;
                    planDetail.TrialIntervalCount = planDetails.BillingCycles[0].Frequency.IntervalCount;
                    planDetail.TrialTotalCycles = planDetails.BillingCycles[0].TotalCycles;
                    planDetail.TrialFixedPrice = planDetails.BillingCycles[0].PricingScheme.FixedPrice?.Value != null
                        ? decimal.Parse(planDetails.BillingCycles[0].PricingScheme.FixedPrice.Value, CultureInfo.InvariantCulture)
                        : 0;

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

                _context.PlanDetails.Add(planDetail);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Detalles del plan {planId} guardados exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al guardar los detalles del plan {planId}");
                throw;
            }
        }
       
    }
}
