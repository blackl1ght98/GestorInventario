using GestorInventario.enums;

namespace GestorInventario.Application.DTOS.Paypal.Responses.POST.Order
{
    public record TrackingItemDto
    {
        public string Name { get; init; } 
        public string Sku { get; init; } 
        public int Quantity { get; init; }
        public BarcodeType BarcodeType { get; init; }
        public string BarcodeCode { get; init; } 
        public string ImageUrl { get; init; } 
        public string Url { get; init; } 
    }
}
