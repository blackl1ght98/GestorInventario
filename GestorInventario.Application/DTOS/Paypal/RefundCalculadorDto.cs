namespace GestorInventario.Application.DTOS.Paypal;

public record RefundCalculadorDto
{
    public decimal MontoReembolso { get; init; }
    public decimal MontoDisponible { get; init; }
    public string Estado { get; init; }
}