namespace GestorInventario.Application.DTOS.Pedidos;
//Esto a cada producto
public class DetallePedidoDto
{
    public int DetalleId { get; set; }
    public string NombreProducto { get; set; }
    public string Descripcion { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal SubtotalSinIva { get; set; }
    public decimal Iva { get; set; }
    public decimal Total { get; set; }
    public bool Rembolsado { get; set; }
}
//Esto representa al pedido completo
public class DetalleOrdenDto
{
    public DateTime FechaPedido { get; set; }
    public string NombreCompleto { get; set; }
    public string NumeroPedido { get; set; }
    public string EstadoPedido { get; set; }
    public string Currency { get; set; }
    public List<DetallePedidoDto> DetallePedidoDtos { get; set; }
    public decimal TotalSinIva { get; set; }
    public decimal TotalIva { get; set; }
    public decimal Total { get; set; }
    public string PaymentId { get; set; }
    public int  PedidoId { get; set; }
    
}