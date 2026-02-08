using GestorInventario.Domain.Models;

namespace GestorInventario.Application.DTOs
{
    public class CarritoConItemsDto
    {
        public Pedido Carrito { get; set; }
        public List<DetallePedido> Items { get; set; } = new();
    }
}
