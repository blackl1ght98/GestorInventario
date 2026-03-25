using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using System.Globalization;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class SubscriptionService:ISubscriptionService
    {
        private readonly GestorInventarioContext _context;
        private readonly IPaypalService _paypalSevice;
        private readonly IPaypalRepository _paypalRepository;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(GestorInventarioContext context, IPaypalService paypalSevice, IPaypalRepository paypalRepository, ILogger<SubscriptionService> logger)
        {
            _context = context;
            _paypalSevice = paypalSevice;
            _paypalRepository = paypalRepository;
            _logger = logger;
        }

        public async Task<SubscriptionDetail> CreateSubscriptionDetailAsync(dynamic subscriptionDetails, string planId)
        {
           
            try
            {
                var plan = await _paypalRepository.ObtenerPlan(planId);

                var minSqlDate = new DateTime(1753, 1, 1);

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

                    // ← Usamos PRIMERO el valor que viene de PayPal
                    NextBillingTime = subscriptionDetails.BillingInfo?.NextBillingTime ?? minSqlDate,

                    LastPaymentTime = subscriptionDetails.BillingInfo?.LastPayment?.Time ?? minSqlDate,
                    LastPaymentAmount = subscriptionDetails.BillingInfo?.LastPayment?.Amount?.Value != null
                        ? Convert.ToDecimal(subscriptionDetails.BillingInfo.LastPayment.Amount.Value) : 0,
                    LastPaymentCurrency = subscriptionDetails.BillingInfo?.LastPayment?.Amount?.CurrencyCode ?? string.Empty,
                    FinalPaymentTime = subscriptionDetails.BillingInfo?.FinalPaymentTime ?? minSqlDate,

                    CyclesCompleted = subscriptionDetails.BillingInfo?.CycleExecutions != null && subscriptionDetails.BillingInfo.CycleExecutions.Count > 0
                        ? subscriptionDetails.BillingInfo.CycleExecutions[0].CyclesCompleted : 0,

                    CyclesRemaining = subscriptionDetails.BillingInfo?.CycleExecutions != null && subscriptionDetails.BillingInfo.CycleExecutions.Count > 0
                        ? subscriptionDetails.BillingInfo.CycleExecutions[0].CyclesRemaining : 0,

                    TotalCycles = subscriptionDetails.BillingInfo?.CycleExecutions != null && subscriptionDetails.BillingInfo.CycleExecutions.Count > 0
                        ? subscriptionDetails.BillingInfo.CycleExecutions[0].TotalCycles : 0,

                    TrialIntervalUnit = plan?.TrialIntervalUnit,
                    TrialIntervalCount = plan?.TrialIntervalCount ?? 0,
                    TrialTotalCycles = plan?.TrialTotalCycles ?? 0,
                    TrialFixedPrice = plan?.TrialFixedPrice ?? 0
                };

                // ================================================
                // RECÁLCULO DE NEXT BILLING TIME 
                // ================================================
                bool shouldRecalculateNextBilling =
                    detallesSuscripcion.Status == "ACTIVE" &&
                    detallesSuscripcion.CyclesCompleted == 1 &&                    // Estamos en el primer ciclo (trial)
                    detallesSuscripcion.CyclesRemaining == 0 &&                    // Ya se completó el trial
                    plan?.TrialTotalCycles > 0 &&                                  // El plan realmente tenía trial
                    (detallesSuscripcion.NextBillingTime == minSqlDate ||          // PayPal no devolvió fecha
                     detallesSuscripcion.NextBillingTime < DateTime.UtcNow.AddDays(7)); // O la fecha es muy cercana/pasada

                if (shouldRecalculateNextBilling)
                {
                    detallesSuscripcion.NextBillingTime = detallesSuscripcion.StartTime.AddDays(
                        (double)detallesSuscripcion.TrialIntervalCount * (double)detallesSuscripcion.TrialTotalCycles + 1);

                    _logger.LogInformation("Recalculando NextBillingTime después del trial para suscripción {SubscriptionId}",
                        detallesSuscripcion.SubscriptionId);
                }
                // Si no se cumple la condición → respetamos estrictamente lo que vino de PayPal

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
