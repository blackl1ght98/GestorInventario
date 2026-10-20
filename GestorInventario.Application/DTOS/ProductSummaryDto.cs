namespace GestorInventario.Application.DTOS;

public class ProductSummaryDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}

public class ProductsPageDto
{
    public List<ProductSummaryDto> Products { get; init; } = new();
    public bool HasNextPage { get; init; }
}