using GestorInventario.Domain.Models;

namespace GestorInventario.Application.DTOs
{
    public class CarritoConItems
    {
        public Pedido Carrito { get; init; }
        public List<DetallePedido> Items { get; init; } = new();
    }
}
