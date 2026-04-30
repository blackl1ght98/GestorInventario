using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class PayPalPaymentCapture
{
    public int Id { get; set; }

    public string PaymentId { get; set; } = null!;

    public string CaptureId { get; set; } = null!;

    public string? Status { get; set; }

    public decimal? Amount { get; set; }

    public string? Currency { get; set; }

    public string? ProtectionEligibility { get; set; }

    public decimal? TransactionFeeAmount { get; set; }

    public string? TransactionFeeCurrency { get; set; }

    public decimal? ReceivableAmount { get; set; }

    public string? ReceivableCurrency { get; set; }

    public decimal? ExchangeRate { get; set; }

    public bool? FinalCapture { get; set; }

    public string? DisputeCategories { get; set; }

    public DateTime? CreateTime { get; set; }

    public DateTime? UpdateTime { get; set; }

    public virtual PayPalPaymentDetail Payment { get; set; } = null!;
}
