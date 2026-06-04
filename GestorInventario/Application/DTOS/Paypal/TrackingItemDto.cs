using GestorInventario.enums;

namespace GestorInventario.Application.DTOS.Paypal
{
    public class TrackingItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public BarcodeType BarcodeType { get; set; }
        public string BarcodeCode { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
