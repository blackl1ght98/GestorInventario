namespace GestorInventario.Application.DTOS;

public class TrackingItemDto
{
    public required string Name { get; init; }
    public string? Sku { get; init; }
    public int Quantity { get; init; }
    public string? BarcodeType { get; init; }
    public string? BarcodeCode { get; init; }
    public string? ImageUrl { get; init; }
    public string? Url { get; init; }
}