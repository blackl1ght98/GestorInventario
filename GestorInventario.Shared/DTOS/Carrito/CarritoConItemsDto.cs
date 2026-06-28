using GestorInventario.Domain.Models;

namespace GestorInventario.Shared.DTOS.Carrito
{
    public class CarritoConItemsDto
    {
        public Pedido Carrito { get; set; }
        public List<DetallePedido> Items { get; set; } = new();
    }
}
