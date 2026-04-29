namespace GestorInventario.ViewModels
{
    internal class PayPalPaymentItemViewModel
    {
        public string ItemName { get; set; }
        public string ItemSku { get; set; }
        public decimal? ItemPrice { get; set; }
        public string ItemCurrency { get; set; }
        public decimal? ItemTax { get; set; }
        public decimal? ItemQuantity { get; set; }
    }
}