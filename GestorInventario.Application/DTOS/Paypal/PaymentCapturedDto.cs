namespace GestorInventario.Application.DTOS.Paypal;

public record PaymentCapturedDto
{
    public required string CaptureId { get; init; }
    public decimal Total { get; init; }
    public required string Currency { get; init; }
}