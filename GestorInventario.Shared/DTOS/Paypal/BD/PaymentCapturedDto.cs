namespace GestorInventario.Shared.DTOS.Paypal.BD;

public class PaymentCapturedDto
{
    public required string CaptureId { get; set; }
    public decimal Total { get; set; }
    public required string Currency { get; set; }
}