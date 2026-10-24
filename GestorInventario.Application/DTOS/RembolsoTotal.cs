namespace GestorInventario.Application.DTOS;

public class RembolsoTotal
{
    public List<int> DetallesIds { get; set; }
    public decimal AmountRefunded { get; set; }
    public int PedidoId { get; set; }
}