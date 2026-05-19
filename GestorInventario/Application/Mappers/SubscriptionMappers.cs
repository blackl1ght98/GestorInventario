using GestorInventario.Application.DTOS.Paypal.Responses.GET.Subscription;
using GestorInventario.Domain.Models;
using System.Globalization;

namespace GestorInventario.Application.Mappers
{
    public static class SubscriptionMappers
    {
        private static readonly DateTime MinSqlDate = new DateTime(1753, 1, 1);
        /// <summary>
        /// Compara una suscripción existente con datos nuevos de PayPal.
        /// Si hay cambios, actualiza la entidad existente y devuelve true.
        /// Si son idénticos, devuelve false sin tocar nada.
        /// </summary>
        public static bool TryUpdateFromPayPal(
            SubscriptionDetail existing,
            SubscriptionDetail incoming)
        {
            bool hasChanges = !AreEqual(existing, incoming);

            if (!hasChanges)
                return false;

            // Transferir solo si cambió
            existing.PlanId = incoming.PlanId;
            existing.Status = incoming.Status;
            existing.StartTime = incoming.StartTime;
            existing.StatusUpdateTime = incoming.StatusUpdateTime;
            existing.SubscriberName = incoming.SubscriberName;
            existing.SubscriberEmail = incoming.SubscriberEmail;
            existing.PayerId = incoming.PayerId;
            existing.OutstandingBalance = incoming.OutstandingBalance;
            existing.OutstandingCurrency = incoming.OutstandingCurrency;
            existing.NextBillingTime = incoming.NextBillingTime;
            existing.LastPaymentTime = incoming.LastPaymentTime;
            existing.LastPaymentAmount = incoming.LastPaymentAmount;
            existing.LastPaymentCurrency = incoming.LastPaymentCurrency;
            existing.FinalPaymentTime = incoming.FinalPaymentTime;
            existing.CyclesCompleted = incoming.CyclesCompleted;
            existing.CyclesRemaining = incoming.CyclesRemaining;
            existing.TotalCycles = incoming.TotalCycles;
            existing.TrialIntervalUnit = incoming.TrialIntervalUnit;
            existing.TrialIntervalCount = incoming.TrialIntervalCount;
            existing.TrialTotalCycles = incoming.TrialTotalCycles;
            existing.TrialFixedPrice = incoming.TrialFixedPrice;

            return true;
        }

        private static bool AreEqual(SubscriptionDetail a, SubscriptionDetail b)
        {
            return a.PlanId == b.PlanId
                && a.Status == b.Status
                && a.StartTime == b.StartTime
                && a.StatusUpdateTime == b.StatusUpdateTime
                && a.SubscriberName == b.SubscriberName
                && a.SubscriberEmail == b.SubscriberEmail
                && a.PayerId == b.PayerId
                && a.OutstandingBalance == b.OutstandingBalance
                && a.OutstandingCurrency == b.OutstandingCurrency
                && a.NextBillingTime == b.NextBillingTime
                && a.LastPaymentTime == b.LastPaymentTime
                && a.LastPaymentAmount == b.LastPaymentAmount
                && a.LastPaymentCurrency == b.LastPaymentCurrency
                && a.FinalPaymentTime == b.FinalPaymentTime
                && a.CyclesCompleted == b.CyclesCompleted
                && a.CyclesRemaining == b.CyclesRemaining
                && a.TotalCycles == b.TotalCycles
                && a.TrialIntervalUnit == b.TrialIntervalUnit
                && a.TrialIntervalCount == b.TrialIntervalCount
                && a.TrialTotalCycles == b.TrialTotalCycles
                && a.TrialFixedPrice == b.TrialFixedPrice;
        }
        public static SubscriptionDetail MapPayPalSubscriptionToEntity(
        PaypalSubscriptionResponse paypalResponse,
        PlanDetail? plan)
        {
            var billingInfo = paypalResponse.BillingInfo;

            var entity = new SubscriptionDetail
            {
                // ── Identificación ──
                SubscriptionId = paypalResponse.Id ?? string.Empty,
                PlanId = paypalResponse.PlanId,

                // ── Estado y fechas ──
                Status = paypalResponse.Status,
                StartTime = paypalResponse.StartTime ?? MinSqlDate,
                StatusUpdateTime = paypalResponse.StatusUpdateTime ?? MinSqlDate,

                // ── Suscriptor ──
                SubscriberName = BuildSubscriberName(paypalResponse.Subscriber),
                SubscriberEmail = paypalResponse.Subscriber?.EmailAddress ?? string.Empty,
                PayerId = paypalResponse.Subscriber?.PayerId ?? string.Empty,

                // ── Balance pendiente ──
                OutstandingBalance = ParseDecimal(billingInfo?.OutstandingBalance?.Value),
                OutstandingCurrency = billingInfo?.OutstandingBalance?.CurrencyCode ?? string.Empty,

                // ── Próximo pago ──
                NextBillingTime = billingInfo?.NextBillingTime ?? MinSqlDate,

                // ── Último pago ──
                LastPaymentTime = billingInfo?.LastPayment?.Time ?? MinSqlDate,
                LastPaymentAmount = ParseDecimal(billingInfo?.LastPayment?.Amount?.Value),
                LastPaymentCurrency = billingInfo?.LastPayment?.Amount?.CurrencyCode ?? string.Empty,

                // ── Ciclos (regular = último) ──
                CyclesCompleted = GetLastCycleValue(billingInfo?.CycleExecutions, c => c.CyclesCompleted),
                CyclesRemaining = GetLastCycleValue(billingInfo?.CycleExecutions, c => c.CyclesRemaining),
                TotalCycles = GetLastCycleValue(billingInfo?.CycleExecutions, c => c.TotalCycles),

                // ── Periodo de prueba (desde plan BD + ciclos PayPal) ──
                TrialIntervalUnit = plan?.TrialIntervalUnit,
                TrialIntervalCount = plan?.TrialIntervalCount ?? 0,
                TrialCyclesCompleted = GetFirstCycleValue(billingInfo?.CycleExecutions, c => c.CyclesCompleted),
                TrialCyclesRemaining = GetFirstCycleValue(billingInfo?.CycleExecutions, c => c.CyclesRemaining),
                TrialTotalCycles = plan?.TrialTotalCycles ?? 0,
                TrialFixedPrice = plan?.TrialFixedPrice ?? 0,

                // ── Fecha final de pago (calculada) ──
                FinalPaymentTime = CalculateFinalPaymentTime(billingInfo, plan)
            };

            return entity;
        }

        // ── Helpers privados ──

        private static string BuildSubscriberName(Subscriber? subscriber)
        {
            if (subscriber?.Name == null)
                return string.Empty;

            return $"{subscriber.Name.GivenName ?? ""} {subscriber.Name.Surname ?? ""}".Trim();
        }

        private static decimal ParseDecimal(string? value)
        {
            return value != null && decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0;
        }

        private static int GetLastCycleValue(List<CycleExecution>? cycles, Func<CycleExecution, int> selector)
        {
            return cycles?.LastOrDefault() != null ? selector(cycles.Last()) : 0;
        }

        private static int GetFirstCycleValue(List<CycleExecution>? cycles, Func<CycleExecution, int> selector)
        {
            return cycles?.FirstOrDefault() != null ? selector(cycles.First()) : 0;
        }

        private static DateTime CalculateFinalPaymentTime(BillingInfo? billingInfo, PlanDetail? plan)
        {
            // Si PayPal envía fecha final explícita, usarla
            if (billingInfo?.FinalPaymentTime != null)
                return billingInfo.FinalPaymentTime.Value;

            // Calcular desde ciclos totales
            var totalCycles = billingInfo?.CycleExecutions?.LastOrDefault()?.TotalCycles
                            ?? plan?.TrialTotalCycles
                            ?? 0;

            // Plan indefinido (0 ciclos = infinito)
            if (totalCycles == 0)
                return DateTime.UtcNow.AddYears(10);

            // Aproximación: ciclos mensuales desde fecha de inicio
            var startTime = billingInfo?.NextBillingTime ?? DateTime.UtcNow;
            return startTime.AddMonths(totalCycles);
        }
    }
}

