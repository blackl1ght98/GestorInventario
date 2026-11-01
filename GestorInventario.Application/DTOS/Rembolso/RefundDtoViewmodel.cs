namespace GestorInventario.Application.DTOS.Rembolso;

public class RefundDtoViewmodel
{
    public string NombreCliente { get; set; }
    public string EmailCliente { get; set; }
    public DateTime? FechaRembolso { get; set; }
    public string  NumeroPedido { get; set; }
    public string EstadoRembolso { get; set; }
    public string MotivoRembolso { get; set; }
    public int Id { get; set; }
    public string Currency { get; set; }
    public string EstadoPedido { get; set; }
}