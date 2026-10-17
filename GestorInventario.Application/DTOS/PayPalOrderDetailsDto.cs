namespace GestorInventario.Application.DTOS;

public class PayPalOrderDetailsDto
{
    public required string OrderId { get; init; }
    public required string Intent { get; init; }
    public required string Status { get; init; }

    public string? PayerEmail { get; init; }
    public string? PayerFirstName { get; init; }
    public string? PayerLastName { get; init; }
    public string? PayerId { get; init; }

    public string? Description { get; init; }
    public string? PayeeMerchantId { get; init; }
    public string? PayeeEmail { get; init; }

    public decimal AmountTotal { get; init; }
    public string? AmountCurrency { get; init; }
    public decimal AmountItemTotal { get; init; }
    public decimal AmountShipping { get; init; }
    public decimal AmountTaxTotal { get; init; }

    public PayPalShippingDto? Shipping { get; init; }
    public List<PayPalCaptureDto> Captures { get; init; } = new();
    public List<PayPalRefundDto> Refunds { get; init; } = new();
    public List<PayPalItemDto> Items { get; init; } = new();
}

public class PayPalShippingDto
{
    public string? RecipientName { get; init; }
    public string? AddressLine1 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? CountryCode { get; init; }
}

public class PayPalCaptureDto
{
    public required string CaptureId { get; init; }
    public required string Status { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public bool FinalCapture { get; init; }
    public string? ProtectionEligibility { get; init; }
    public decimal TransactionFeeAmount { get; init; }
    public string? TransactionFeeCurrency { get; init; }
    public decimal ReceivableAmount { get; init; }
    public string? ReceivableCurrency { get; init; }
    public decimal? ExchangeRate { get; init; }
    public string? DisputeCategoriesJson { get; init; }
    public DateTime? CreateTime { get; init; }
    public DateTime? UpdateTime { get; init; }
    public string? InvoiceId { get; init; }
}

public class PayPalRefundDto
{
    public required string RefundId { get; init; }
    public required string Status { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
    public string? NoteToPayer { get; init; }
    public string? InvoiceId { get; init; }
    public DateTime? CreateTime { get; init; }
    public DateTime? UpdateTime { get; init; }
}

public class PayPalItemDto
{
    public required string Name { get; init; }
    public string? Sku { get; init; }
    public decimal UnitAmount { get; init; }
    public decimal Tax { get; init; }
    public int Quantity { get; init; }
    public string? Currency { get; init; }
}