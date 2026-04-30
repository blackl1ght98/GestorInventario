using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class PayPalPaymentDetail
{
    public string Id { get; set; } = null!;

    public string? Intent { get; set; }

    public string? Status { get; set; }

    public string? PayerEmail { get; set; }

    public string? PayerFirstName { get; set; }

    public string? PayerLastName { get; set; }

    public string? PayerId { get; set; }

    public decimal? AmountTotal { get; set; }

    public string? AmountCurrency { get; set; }

    public decimal? AmountItemTotal { get; set; }

    public decimal? AmountShipping { get; set; }

    public string? PayeeMerchantId { get; set; }

    public string? PayeeEmail { get; set; }

    public string? Description { get; set; }

    public DateTime? CreateTime { get; set; }

    public DateTime? UpdateTime { get; set; }

    public string? TrackingId { get; set; }

    public string? TrackingStatus { get; set; }

    public virtual ICollection<PayPalPaymentCapture> PayPalPaymentCaptures { get; set; } = new List<PayPalPaymentCapture>();

    public virtual ICollection<PayPalPaymentItem> PayPalPaymentItems { get; set; } = new List<PayPalPaymentItem>();

    public virtual ICollection<PayPalPaymentShipping> PayPalPaymentShippings { get; set; } = new List<PayPalPaymentShipping>();
}
