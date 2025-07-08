using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GestorInventario.Infraestructure.Repositories
{
    public class PaypalRepository:IPaypalRepository
    {
        public readonly GestorInventarioContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaypalRepository> _logger;
        public PaypalRepository(GestorInventarioContext context, IUnitOfWork unitOfWork, ILogger<PaypalRepository> logger)
        {
            _context = context;
            _unitOfWork = unitOfWork;
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
                var planRequest = await _unitOfWork.PaypalService.ObtenerDetallesPlan(id);
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
       
    }
}
