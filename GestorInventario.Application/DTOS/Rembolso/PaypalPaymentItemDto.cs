namespace GestorInventario.Application.DTOS.Rembolso
{
    public class PaypalPaymentItemDto
    {
        public string ItemName { get; set; }
        public int ItemQuantity { get; set; }
        public decimal ItemPrice { get; set; }
        public string ItemCurrency { get; set; }
        public string ItemSku { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal SubtotalSinIva {  get; set; }
        public decimal Iva {  get; set; }
        public decimal TotalConIva { get; set; }
    }
}
