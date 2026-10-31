namespace GestorInventario.Application.DTOS.Paypal;

public record ProductSummaryDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}

public record ProductsPageDto
{
    public List<ProductSummaryDto> Products { get; init; } = new();
    public bool HasNextPage { get; init; }
}