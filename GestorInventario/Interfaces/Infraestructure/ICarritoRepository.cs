using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface ICarritoRepository
    {
        
        Task<Pedido> ObtenerCarritoUsuario(int userId);
        Task<List<DetallePedido>> ObtenerItemsDelCarritoUsuario(int pedidoId);
        Task<DetallePedido> ItemsDelCarrito(int id);
        IQueryable<DetallePedido> ObtenerItems(int pedidoId);
        Task<List<Monedum>> ObtenerMoneda();
        Task<Pedido> CrearCarritoUsuario(int userId);
        Task<(bool, string, string)> PagarV2(string moneda, int userId);
        Task<(bool, string)> Incremento(int id);
        Task<(bool, string)> Decremento(int id);
        Task<(bool, string)> EliminarProductoCarrito(int id);

    }
}
