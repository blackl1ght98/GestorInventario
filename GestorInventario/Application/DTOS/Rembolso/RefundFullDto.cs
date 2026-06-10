namespace GestorInventario.Application.DTOS.Rembolso
{
    public class RefundFullDto
    {
        public required int PedidoId { get; set; }
        public required string Currency { get; set; }
       
    }
}
