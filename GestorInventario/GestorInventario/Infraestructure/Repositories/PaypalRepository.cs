using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GestorInventario.Infraestructure.Repositories
{
    public class PaypalRepository:IPaypalController
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
                    PaypalPlanId = planRequest.id,
                    ProductId = planRequest.product_id,
                    Name = planRequest.name,
                    Description = planRequest.description,
                    Status = planRequest.status,
                    AutoBillOutstanding = planRequest.payment_preferences.auto_bill_outstanding,
                    SetupFee = planRequest.payment_preferences.setup_fee?.value != null ?
                decimal.Parse(planRequest.payment_preferences.setup_fee.value.ToString(), CultureInfo.InvariantCulture) : 0,
                    SetupFeeFailureAction = planRequest.payment_preferences.setup_fee_failure_action,
                    PaymentFailureThreshold = planRequest.payment_preferences.payment_failure_threshold,
                    TaxPercentage = planRequest.taxes?.percentage != null ?
                     decimal.Parse(planRequest.taxes.percentage.ToString(), CultureInfo.InvariantCulture) : 0,
                    TaxInclusive = planRequest.taxes?.inclusive ?? false
                };



                // Verificar si existe un ciclo de facturación de prueba si esta condicion se cumple es que existe dias de prueba.
                if (planRequest.billing_cycles.Count > 1)
                {
                    planDetails.TrialIntervalUnit = planRequest.billing_cycles[0].frequency.interval_unit;
                    planDetails.TrialIntervalCount = planRequest.billing_cycles[0].frequency.interval_count;
                    planDetails.TrialTotalCycles = planRequest.billing_cycles[0].total_cycles;

                    // Convertir TrialFixedPrice a decimal si no es nulo
                    planDetails.TrialFixedPrice = planRequest.billing_cycles[0].pricing_scheme.fixed_price?.value != null ?
                                                  decimal.Parse(planRequest.billing_cycles[0].pricing_scheme.fixed_price.value.ToString(), CultureInfo.InvariantCulture) : 0;

                    // Información del ciclo regular
                    planDetails.RegularIntervalUnit = planRequest.billing_cycles[1].frequency.interval_unit;
                    planDetails.RegularIntervalCount = planRequest.billing_cycles[1].frequency.interval_count;
                    planDetails.RegularTotalCycles = planRequest.billing_cycles[1].total_cycles;

                    // Convertir RegularFixedPrice a decimal si no es nulo
                    planDetails.RegularFixedPrice = planRequest.billing_cycles[1].pricing_scheme.fixed_price?.value != null ?
                                                    decimal.Parse(planRequest.billing_cycles[1].pricing_scheme.fixed_price.value.ToString(), CultureInfo.InvariantCulture) : 0;
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
            catch (Exception ex)
            {
                _logger.LogError($"Error al guardar los detalles del plan en la base de datos: {ex.Message}", ex);

            }
        }
       
    }
}
