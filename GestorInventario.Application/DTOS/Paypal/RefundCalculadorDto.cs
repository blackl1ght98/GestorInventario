namespace GestorInventario.Application.DTOS.Paypal;

public class RefundCalculadorDto
{
    public decimal MontoReembolso { get; set; }
    public decimal MontoDisponible { get; set; }
    public string Estado { get; set; }
}