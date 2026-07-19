using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class PayPalPaymentDetail
{
    public string Id { get; set; } = null!;

    public string Intent { get; set; } = null!;

    public string OrderStatus { get; set; } = null!;

    public string PayerEmail { get; set; } = null!;

    public string PayerFirstName { get; set; } = null!;

    public string PayerLastName { get; set; } = null!;

    public string PayerId { get; set; } = null!;

    public decimal AmountTotal { get; set; }

    public string AmountCurrency { get; set; } = null!;

    public decimal AmountItemTotal { get; set; }

    public decimal AmountShipping { get; set; }

    public string PayeeMerchantId { get; set; } = null!;

    public string PayeeEmail { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }

    public decimal AmountTax { get; set; }

    public virtual ICollection<PayPalPaymentCapture> PayPalPaymentCaptures { get; set; } = new List<PayPalPaymentCapture>();

    public virtual ICollection<PayPalPaymentItem> PayPalPaymentItems { get; set; } = new List<PayPalPaymentItem>();

    public virtual ICollection<PayPalPaymentRefund> PayPalPaymentRefunds { get; set; } = new List<PayPalPaymentRefund>();

    public virtual ICollection<PayPalPaymentShipping> PayPalPaymentShippings { get; set; } = new List<PayPalPaymentShipping>();
}
