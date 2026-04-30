using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class PayPalPaymentShipping
{
    public int Id { get; set; }

    public string PaymentId { get; set; } = null!;

    public string? RecipientName { get; set; }

    public string? AddressLine1 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? CountryCode { get; set; }

    public virtual PayPalPaymentDetail Payment { get; set; } = null!;
}
