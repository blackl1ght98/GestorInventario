namespace GestorInventario.Application.DTOS;



public class CreateOrderResultDto
{
    public required string OrderId { get; init; }
    public required string ApprovalUrl { get; init; }
    public string? Status { get; init; }
}