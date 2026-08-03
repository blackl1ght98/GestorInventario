using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class PayPalPaymentShipping
{
    public int Id { get; set; }

    public string PaymentId { get; set; } = null!;

    public string RecipientName { get; set; } = null!;

    public string AddressLine1 { get; set; } = null!;

    public string City { get; set; } = null!;

    public string State { get; set; } = null!;

    public string PostalCode { get; set; } = null!;

    public string CountryCode { get; set; } = null!;

    public virtual PayPalPaymentDetail Payment { get; set; } = null!;
}
