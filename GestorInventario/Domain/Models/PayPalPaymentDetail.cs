using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class PayPalPaymentDetail
{
    public string Id { get; set; } = null!;

    public string? Intent { get; set; }

    public string? Status { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PayerEmail { get; set; }

    public string? PayerFirstName { get; set; }

    public string? PayerLastName { get; set; }

    public string? PayerId { get; set; }

    public string? ShippingRecipientName { get; set; }

    public string? ShippingLine1 { get; set; }

    public string? ShippingCity { get; set; }

    public string? ShippingState { get; set; }

    public string? ShippingPostalCode { get; set; }

    public string? ShippingCountryCode { get; set; }

    public decimal? AmountTotal { get; set; }

    public string? AmountCurrency { get; set; }

    public decimal? AmountItemTotal { get; set; }

    public decimal? AmountShipping { get; set; }

    public string? PayeeMerchantId { get; set; }

    public string? PayeeEmail { get; set; }

    public string? Description { get; set; }

    public string? SaleId { get; set; }

    public string? CaptureStatus { get; set; }

    public decimal? CaptureAmount { get; set; }

    public string? CaptureCurrency { get; set; }

    public string? ProtectionEligibility { get; set; }

    public decimal? TransactionFeeAmount { get; set; }

    public string? TransactionFeeCurrency { get; set; }

    public decimal? ReceivableAmount { get; set; }

    public string? ReceivableCurrency { get; set; }

    public decimal? ExchangeRate { get; set; }

    public DateTime? CreateTime { get; set; }

    public DateTime? UpdateTime { get; set; }

    public string? TrackingId { get; set; }

    public string? TrackingStatus { get; set; }

    public bool? FinalCapture { get; set; }

    public string? DisputeCategories { get; set; }

    public virtual ICollection<PayPalPaymentItem> PayPalPaymentItems { get; set; } = new List<PayPalPaymentItem>();
}
