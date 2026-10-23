using GestorInventario.Domain.Models;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Mappers;

public class PaypalSubscriptionMapper
{
    public static SubcriptionDetailViewModel ToViewModel(SubscriptionDetail details)
    {
        if (details == null) return null;

        // Lógica de negocio: Cálculo de días de prueba
        int trialIntervalCount = details.TrialIntervalCount ?? 0;
        int trialTotalCycles = details.TrialTotalCycles ?? 0;
        int trialDays = trialIntervalCount > 0 ? trialIntervalCount * trialTotalCycles : 0;

        // Lógica de negocio: Determinar si está en período de prueba activo
        bool enPeriodoPruebaActivo =
            details.Status == "ACTIVE" &&
            details.NextBillingTime.HasValue &&
            DateTime.UtcNow < details.NextBillingTime.Value &&
            details.LastPaymentTime <= new DateTime(1900, 1, 1);

        return new SubcriptionDetailViewModel
        {
            SubscriptionId = details.SubscriptionId,
            PlanId = details.PlanId,
            Status = details.Status,
            SubscriberName = details.SubscriberName,
            SubscriberEmail = details.SubscriberEmail,
            PayerId = details.PayerId,
            StartDate = details.StartTime,
            StatusUpdateDate = details.StatusUpdateTime,
            NextBillingTime = details.NextBillingTime,
            LastPaymentTime = details.LastPaymentTime,
            FinalPaymentTime = details.FinalPaymentTime,
            OutstandingBalance = details.OutstandingBalance,
            OutstandingCurrency = details.OutstandingCurrency,
            LastPaymentAmount = details.LastPaymentAmount,
            LastPaymentCurrency = details.LastPaymentCurrency,
            TrialDays = trialDays,
            CyclesCompleted = details.CyclesCompleted,
            CyclesRemaining = details.CyclesRemaining,
            TotalCycles = details.TotalCycles,
            MostrarCiclos = details.CyclesCompleted > 0 ||
                            details.CyclesRemaining > 0 ||
                            details.TotalCycles > 0 ||
                            trialDays > 0,
            EnPeriodoPrueba = enPeriodoPruebaActivo
        };
    }
}