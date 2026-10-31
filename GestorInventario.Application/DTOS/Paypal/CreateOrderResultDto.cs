namespace GestorInventario.Application.DTOS.Paypal;



public record CreateOrderResultDto
{
    public required string OrderId { get; init; }
    public required string ApprovalUrl { get; init; }
    public string? Status { get; init; }
}