namespace GestorInventario.Application.DTOS.Paypal.Projections
{
    public class OrderRefundProjection
    {
        public required string OrderId { get; set; }
        public required string Status { get; set; }

        // Payer (simplificado)
        public string? PayerEmail { get; set; }
        public string? PayerFirstName { get; set; }
        public string? PayerLastName { get; set; }
        public string? PayerId { get; set; }

        // Shipping
        public string? RecipientName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? CountryCode { get; set; }

        // Amounts
        public decimal TotalAmount { get; set; }
        public string? Currency { get; set; }
        public decimal ItemTotal { get; set; }
        public decimal ShippingAmount { get; set; }

        // Payee
        public string? MerchantId { get; set; }
        public string? PayeeEmail { get; set; }

        // Captures (para calcular reembolso disponible)
        public List<CaptureProjection> Captures { get; set; } = new();
        public List<RefundProjection> Refunds { get; set; } = new();
    }

    public class CaptureProjection
    {
        public required string Id { get; set; }
        public required string Status { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public decimal NetAmount { get; set; }
        public decimal PaypalFee { get; set; }
        public string? ExchangeRate { get; set; }
        public bool FinalCapture { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public string? ProtectionStatus { get; set; }
        public List<string>? DisputeCategories { get; set; }
    }

    public class RefundProjection
    {
        public required string Id { get; set; }
        public decimal Amount { get; set; }
        public decimal NetAmount { get; set; }
        public string? Status { get; set; }
    }
}
