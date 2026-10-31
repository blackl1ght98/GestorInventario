namespace GestorInventario.Application.DTOS.Paypal
{
    public record PayPalPaymentItemDto
    {
        public string ItemName { get; init; }
        public string ItemSku { get; init; }
        public decimal? ItemPrice { get; init; }
        public string ItemCurrency { get; init; }
        public decimal? ItemTax { get; init; }
        public decimal? ItemQuantity { get; init; }
    }
}