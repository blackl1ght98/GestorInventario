using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class PayPalPaymentDetails
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

    public virtual ICollection<PayPalPaymentCaptures> PayPalPaymentCaptures { get; set; } = new List<PayPalPaymentCaptures>();

    public virtual ICollection<PayPalPaymentItems> PayPalPaymentItems { get; set; } = new List<PayPalPaymentItems>();

    public virtual ICollection<PayPalPaymentRefunds> PayPalPaymentRefunds { get; set; } = new List<PayPalPaymentRefunds>();

    public virtual ICollection<PayPalPaymentShipping> PayPalPaymentShipping { get; set; } = new List<PayPalPaymentShipping>();
}
