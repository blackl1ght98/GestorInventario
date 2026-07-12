namespace GestorInventario.ViewModels.Paypal
{
    public class PayPalPaymentDetailViewModel
    {
        public string Id { get; init; } = string.Empty;
        public string Intent { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public DateTime? CreateTime { get; init; }
        public DateTime? UpdateTime { get; init; }
        public PayerInfo Payer { get; init; } = new();
        public ShippingInfo Shipping { get; init; } = new();
        public AmountInfo Amount { get; init; } = new();
        public PayeeInfo Payee { get; init; } = new();
        public CaptureInfo Capture { get; init; } = new();
        public List<PayPalPaymentItemDto> Items { get; init; } = new();
    }

    public class PayerInfo
    {
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string PayerId { get; init; } = string.Empty;
    }

    public class ShippingInfo
    {
        public string RecipientName { get; init; } = string.Empty;
        public string Line1 { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public string PostalCode { get; init; } = string.Empty;
        public string CountryCode { get; init; } = string.Empty;
    }

    public class AmountInfo
    {
        public decimal Total { get; init; }
        public string Currency { get; init; } = string.Empty;
        public decimal ItemTotal { get; init; }
        public decimal Shipping { get; init; }
    }

    public class PayeeInfo
    {
        public string MerchantId { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }

    public class CaptureInfo
    {
        public string SaleId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public string ProtectionEligibility { get; init; } = string.Empty;
        public decimal TransactionFeeAmount { get; init; }
        public string TransactionFeeCurrency { get; init; } = string.Empty;
        public decimal ReceivableAmount { get; init; }
        public string ReceivableCurrency { get; init; } = string.Empty;
        public decimal ExchangeRate { get; init; }
        public bool FinalCapture { get; init; }
        public string DisputeCategories { get; init; } = string.Empty;
    }
}