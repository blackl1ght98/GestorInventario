using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Repositories
{
    public interface IShopCartRepository
    {
        //Consultas
        Task<Pedidos> ObtenerCarritoUsuario(int userId);
        Task<List<DetallePedido>> ObtenerItemsDelCarritoUsuario(int pedidoId);
        Task<DetallePedido> ItemsDelCarrito(int id);
        IQueryable<DetallePedido> ObtenerItemsConDetalles(int pedidoId);
        Task<List<Moneda>> ObtenerMoneda();       
        Task<List<Pedidos>> ObtenerCarritosActivosAsync(int userId);
        Task<List<Pedidos>> ObtenerCarritosAbandonadosAsync(DateTime cutoff);
    }
}
