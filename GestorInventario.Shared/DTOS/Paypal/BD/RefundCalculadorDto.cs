namespace GestorInventario.Shared.DTOS.Paypal.BD;

public class RefundCalculadorDto
{
    public decimal MontoReembolso { get; set; }
    public decimal MontoDisponible { get; set; }
    public string Estado { get; set; }
}