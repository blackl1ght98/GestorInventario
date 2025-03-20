using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class PayPalPaymentItem
{
    public int Id { get; set; }

    public string PayPalId { get; set; } = null!;

    public string? ItemName { get; set; }

    public string? ItemSku { get; set; }

    public decimal? ItemPrice { get; set; }

    public string? ItemCurrency { get; set; }

    public decimal? ItemTax { get; set; }

    public int? ItemQuantity { get; set; }

    public string? ItemImageUrl { get; set; }

    public virtual PayPalPaymentDetail PayPal { get; set; } = null!;
}
