using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class PlanDetail
{
    public string Id { get; set; } = null!;

    public string? PaypalPlanId { get; set; }

    public string? ProductId { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public string? TrialIntervalUnit { get; set; }

    public int? TrialIntervalCount { get; set; }

    public int? TrialTotalCycles { get; set; }

    public decimal? TrialFixedPrice { get; set; }

    public string? RegularIntervalUnit { get; set; }

    public int? RegularIntervalCount { get; set; }

    public int? RegularTotalCycles { get; set; }

    public decimal? RegularFixedPrice { get; set; }

    public bool? AutoBillOutstanding { get; set; }

    public decimal? SetupFee { get; set; }

    public string? SetupFeeFailureAction { get; set; }

    public int? PaymentFailureThreshold { get; set; }

    public decimal? TaxPercentage { get; set; }

    public bool? TaxInclusive { get; set; }
}
