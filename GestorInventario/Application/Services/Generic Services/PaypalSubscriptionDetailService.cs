using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;


namespace GestorInventario.Application.Services.Generic_Services
{
    public class PaypalSubscriptionDetailService:IPaypalSubscriptionDetailService
    {
        
       
        private readonly IPaypalRepository _paypalRepository;
        private readonly ILogger<PaypalSubscriptionDetailService> _logger;

        public PaypalSubscriptionDetailService(IPaypalRepository paypalRepository, ILogger<PaypalSubscriptionDetailService> logger)
        {
           
          
            _paypalRepository = paypalRepository;
            _logger = logger;
        }

        public async Task<SubscriptionDetail> CreateSubscriptionDetailAsync(PaypalSubscriptionResponse subscriptionDetails, string planId)
        {
            try
            {
                var plan = await _paypalRepository.ObtenerPlan(planId);
                var minSqlDate = new DateTime(1753, 1, 1);

                // 1. Creamos primero el objeto con los datos básicos
                var detallesSuscripcion = new SubscriptionDetail
                {
                    SubscriptionId = subscriptionDetails.Id ?? string.Empty,
                    PlanId = subscriptionDetails.PlanId ?? string.Empty,
                    Status = subscriptionDetails.Status ?? string.Empty,
                    StartTime = subscriptionDetails.StartTime ?? minSqlDate,
                    StatusUpdateTime = subscriptionDetails.StatusUpdateTime ?? minSqlDate,
                    SubscriberName = $"{subscriptionDetails.Subscriber?.Name?.GivenName ?? ""} {subscriptionDetails.Subscriber?.Name?.Surname ?? ""}".Trim(),
                    SubscriberEmail = subscriptionDetails.Subscriber?.EmailAddress ?? string.Empty,
                    PayerId = subscriptionDetails.Subscriber?.PayerId ?? string.Empty,

                    OutstandingBalance = subscriptionDetails.BillingInfo?.OutstandingBalance?.Value != null
                        ? Convert.ToDecimal(subscriptionDetails.BillingInfo.OutstandingBalance.Value) : 0,

                    OutstandingCurrency = subscriptionDetails.BillingInfo?.OutstandingBalance?.CurrencyCode ?? string.Empty,

                    NextBillingTime = subscriptionDetails.BillingInfo?.NextBillingTime ?? minSqlDate,
                    LastPaymentTime = subscriptionDetails.BillingInfo?.LastPayment?.Time ?? minSqlDate,
                    LastPaymentAmount = subscriptionDetails.BillingInfo?.LastPayment?.Amount?.Value != null
                        ? Convert.ToDecimal(subscriptionDetails.BillingInfo.LastPayment.Amount.Value) : 0,

                    LastPaymentCurrency = subscriptionDetails.BillingInfo?.LastPayment?.Amount?.CurrencyCode ?? string.Empty,

                    CyclesCompleted = subscriptionDetails.BillingInfo?.CycleExecutions?.FirstOrDefault()?.CyclesCompleted ?? 0,
                    CyclesRemaining = subscriptionDetails.BillingInfo?.CycleExecutions?.FirstOrDefault()?.CyclesRemaining ?? 0,
                    TotalCycles = subscriptionDetails.BillingInfo?.CycleExecutions?.FirstOrDefault()?.TotalCycles ?? 0,

                    TrialIntervalUnit = plan?.TrialIntervalUnit,
                    TrialIntervalCount = plan?.TrialIntervalCount ?? 0,
                    TrialTotalCycles = plan?.TrialTotalCycles ?? 0,
                    TrialFixedPrice = plan?.TrialFixedPrice ?? 0
                };

                // 2. Ahora sí podemos calcular FinalPaymentTime 
                if (subscriptionDetails.BillingInfo?.FinalPaymentTime != null)
                {
                    detallesSuscripcion.FinalPaymentTime = subscriptionDetails.BillingInfo.FinalPaymentTime.Value;
                }
                else
                {
                    // Si no viene fecha final (caso común en planes anuales)
                    if (detallesSuscripcion.TotalCycles == 0)
                    {
                        detallesSuscripcion.FinalPaymentTime = DateTime.UtcNow.AddYears(10); // indefinido
                    }
                    else
                    {
                        // Cálculo aproximado si tiene ciclos limitados
                        detallesSuscripcion.FinalPaymentTime = detallesSuscripcion.StartTime.AddMonths(
                            detallesSuscripcion.TotalCycles * 12);
                    }
                }

                // 3. Recálculo de NextBillingTime (solo si es necesario)
                bool shouldRecalculateNextBilling =
                    detallesSuscripcion.Status == "ACTIVE" &&
                    detallesSuscripcion.CyclesCompleted == 1 &&
                    detallesSuscripcion.CyclesRemaining == 0 &&
                    plan?.TrialTotalCycles > 0 &&
                    (detallesSuscripcion.NextBillingTime == minSqlDate ||
                     detallesSuscripcion.NextBillingTime < DateTime.UtcNow.AddDays(7));

                if (shouldRecalculateNextBilling)
                {
                    detallesSuscripcion.NextBillingTime = detallesSuscripcion.StartTime.AddDays(
                        (double)detallesSuscripcion.TrialIntervalCount * (double)detallesSuscripcion.TrialTotalCycles + 1);

                    _logger.LogInformation("Recalculando NextBillingTime después del trial para suscripción {SubscriptionId}",
                        detallesSuscripcion.SubscriptionId);
                }

                // 4. Guardar en base de datos
                await _paypalRepository.SaveOrUpdateSubscriptionDetailsAsync(detallesSuscripcion);

                return detallesSuscripcion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear detalles de suscripción para planId {planId}");
                throw;
            }
        }
    }
}
