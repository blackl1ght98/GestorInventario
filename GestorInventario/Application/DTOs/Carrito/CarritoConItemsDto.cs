using GestorInventario.Domain.Models;

namespace GestorInventario.Application.DTOs.Carrito
{
    public class CarritoConItemsDto
    {
        public Pedido Carrito { get; set; }
        public List<DetallePedido> Items { get; set; } = new();
    }
}
