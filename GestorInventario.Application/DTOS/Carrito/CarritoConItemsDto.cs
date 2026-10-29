using GestorInventario.Domain.Models;

namespace GestorInventario.Application.DTOS.Carrito
{
    public record CarritoConItemsDto
    {
        public required Pedido Carrito { get; init; }
        public List<DetallePedido> Items { get; init; } = new();
    }
}
