using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class SubscriptionDetail
{
    public string SubscriptionId { get; set; } = null!;

    public string PlanId { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public DateTime StatusUpdateTime { get; set; }

    public string SubscriberName { get; set; } = null!;

    public string SubscriberEmail { get; set; } = null!;

    public string PayerId { get; set; } = null!;

    public decimal OutstandingBalance { get; set; }

    public string OutstandingCurrency { get; set; } = null!;

    public DateTime? NextBillingTime { get; set; }

    public DateTime? LastPaymentTime { get; set; }

    public decimal? LastPaymentAmount { get; set; }

    public string? LastPaymentCurrency { get; set; }

    public DateTime FinalPaymentTime { get; set; }

    public int CyclesCompleted { get; set; }

    public int CyclesRemaining { get; set; }

    public int TotalCycles { get; set; }

    public string? TrialIntervalUnit { get; set; }

    public int? TrialIntervalCount { get; set; }

    public int? TrialTotalCycles { get; set; }

    public decimal? TrialFixedPrice { get; set; }
}
