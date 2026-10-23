namespace GestorInventario.Application.DTOS.Paypal;

public class PaymentCapturedDto
{
    public required string CaptureId { get; set; }
    public decimal Total { get; set; }
    public required string Currency { get; set; }
}