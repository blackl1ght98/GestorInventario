using GestorInventario.Domain.Models;

namespace GestorInventario.Application.DTOS.Carrito
{
    public class CarritoConItemsDto
    {
        public required Pedido Carrito { get; set; }
        public List<DetallePedido> Items { get; set; } = new();
    }
}
