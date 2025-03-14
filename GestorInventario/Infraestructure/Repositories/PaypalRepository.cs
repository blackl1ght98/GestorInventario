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
        public async Task<(bool,string, SubscriptionDetail)> ObtenerDetallesSubscripcion(string id) {
            try
            {
                // Obtener detalles de la suscripción desde PayPal
                var subscriptionDetails = await  _unitOfWork.PaypalService.ObtenerDetallesSuscripcion(id);

                // Convertir plan_id a string para evitar problemas con árboles de expresión
                string planId = (string)subscriptionDetails.plan_id;

                // Obtener los detalles del plan desde la base de datos usando el PlanId
                var plan = await _context.PlanDetails.FirstOrDefaultAsync(p => p.PaypalPlanId == planId);

                if (plan == null)
                {
                    await DetallesSubscripcion(planId);
                }

                // Establecer la fecha mínima SQL
                var minSqlDate = new DateTime(1753, 1, 1);

                // Crear el objeto de detalles de la suscripción
                var detallesSuscripcion = new SubscriptionDetail
                {
                    SubscriptionId = subscriptionDetails.id ?? string.Empty,
                    PlanId = subscriptionDetails.plan_id ?? string.Empty,
                    Status = subscriptionDetails.status ?? string.Empty,
                    StartTime = subscriptionDetails.start_time ?? minSqlDate,
                    StatusUpdateTime = subscriptionDetails.status_updated_time ?? minSqlDate,
                    SubscriberName = $"{subscriptionDetails.subscriber.name.given_name ?? string.Empty} {subscriptionDetails.subscriber.name.surname ?? string.Empty}",
                    SubscriberEmail = subscriptionDetails.subscriber.email_address ?? string.Empty,
                    PayerId = subscriptionDetails.subscriber.payer_id ?? string.Empty,
                    OutstandingBalance = subscriptionDetails.billing_info.outstanding_balance.value != null ? Convert.ToDecimal(subscriptionDetails.billing_info.outstanding_balance.value) : 0,
                    OutstandingCurrency = subscriptionDetails.billing_info.outstanding_balance.currency_code ?? string.Empty,
                    NextBillingTime = subscriptionDetails.billing_info.next_billing_time ?? minSqlDate,
                    LastPaymentTime = subscriptionDetails.billing_info.last_payment?.time ?? minSqlDate,
                    LastPaymentAmount = subscriptionDetails.billing_info.last_payment?.amount.value != null ? Convert.ToDecimal(subscriptionDetails.billing_info.last_payment.amount.value) : 0,
                    LastPaymentCurrency = subscriptionDetails.billing_info.last_payment?.amount.currency_code ?? string.Empty,
                    FinalPaymentTime = subscriptionDetails.billing_info.final_payment_time ?? minSqlDate,
                    CyclesCompleted = subscriptionDetails.billing_info.cycle_executions[0]?.cycles_completed ?? 0,
                    CyclesRemaining = subscriptionDetails.billing_info.cycle_executions[0]?.cycles_remaining ?? 0,
                    TotalCycles = subscriptionDetails.billing_info.cycle_executions[0]?.total_cycles ?? 0,

                    // Asignar los valores del plan relacionados con el período de prueba
                    TrialIntervalUnit = plan.TrialIntervalUnit,
                    TrialIntervalCount = plan.TrialIntervalCount ?? 0,
                    TrialTotalCycles = plan.TrialTotalCycles ?? 0,
                    TrialFixedPrice = plan.TrialFixedPrice ?? 0
                };

                // Calcular la fecha del próximo pago después del período de prueba
                if (detallesSuscripcion.Status == "ACTIVE" && detallesSuscripcion.CyclesCompleted == 1 && detallesSuscripcion.CyclesRemaining == 0)
                {
                    detallesSuscripcion.NextBillingTime = detallesSuscripcion.StartTime.AddDays((double)detallesSuscripcion.TrialIntervalCount * (double)detallesSuscripcion.TrialTotalCycles + 1);
                }

                // Verificar si la suscripción ya existe en la base de datos
                var existingSubscription = await  _context.SubscriptionDetails
                    .FirstOrDefaultAsync(s => s.SubscriptionId == detallesSuscripcion.SubscriptionId);

                if (existingSubscription != null)
                {
                    // Comparar los detalles y actualizar solo si han cambiado
                    bool hasChanges = !(
                        existingSubscription.PlanId == detallesSuscripcion.PlanId &&
                        existingSubscription.Status == detallesSuscripcion.Status &&
                        existingSubscription.StartTime == detallesSuscripcion.StartTime &&
                        existingSubscription.StatusUpdateTime == detallesSuscripcion.StatusUpdateTime &&
                        existingSubscription.SubscriberName == detallesSuscripcion.SubscriberName &&
                        existingSubscription.SubscriberEmail == detallesSuscripcion.SubscriberEmail &&
                        existingSubscription.PayerId == detallesSuscripcion.PayerId &&
                        existingSubscription.OutstandingBalance == detallesSuscripcion.OutstandingBalance &&
                        existingSubscription.OutstandingCurrency == detallesSuscripcion.OutstandingCurrency &&
                        existingSubscription.NextBillingTime == detallesSuscripcion.NextBillingTime &&
                        existingSubscription.LastPaymentTime == detallesSuscripcion.LastPaymentTime &&
                        existingSubscription.LastPaymentAmount == detallesSuscripcion.LastPaymentAmount &&
                        existingSubscription.LastPaymentCurrency == detallesSuscripcion.LastPaymentCurrency &&
                        existingSubscription.FinalPaymentTime == detallesSuscripcion.FinalPaymentTime &&
                        existingSubscription.CyclesCompleted == detallesSuscripcion.CyclesCompleted &&
                        existingSubscription.CyclesRemaining == detallesSuscripcion.CyclesRemaining &&
                        existingSubscription.TotalCycles == detallesSuscripcion.TotalCycles &&
                        existingSubscription.TrialIntervalUnit == detallesSuscripcion.TrialIntervalUnit &&
                        existingSubscription.TrialIntervalCount == detallesSuscripcion.TrialIntervalCount &&
                        existingSubscription.TrialTotalCycles == detallesSuscripcion.TrialTotalCycles &&
                        existingSubscription.TrialFixedPrice == detallesSuscripcion.TrialFixedPrice
                    );

                    if (hasChanges)
                    {
                        // Actualizar la suscripción existente
                        _context.SubscriptionDetails.Update(detallesSuscripcion);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Guardar los detalles de la nueva suscripción en la base de datos
                    _context.SubscriptionDetails.Add(detallesSuscripcion);
                    await _context.SaveChangesAsync();
                }

                // Pasar los detalles de la suscripción a la vista
                return (true, "Informacion devuelta con exito", detallesSuscripcion);
            }
            catch (Exception ex)
            {

                return (false, "ocurrio un error",null);
            }
        }
    }
}
