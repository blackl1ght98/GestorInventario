using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Infrastructure.Repositories
{
    public interface IShopCartRepository
    {
        //Consultas
        Task<Pedido> ObtenerCarritoUsuario(int userId);
        Task<List<DetallePedido>> ObtenerItemsDelCarritoUsuario(int pedidoId);
        Task<DetallePedido> ItemsDelCarrito(int id);
        IQueryable<DetallePedido> ObtenerItemsConDetalles(int pedidoId);
        Task<List<Monedum>> ObtenerMoneda();       
        Task<List<Pedido>> ObtenerCarritosActivosAsync(int userId);
        Task<List<Pedido>> ObtenerCarritosAbandonadosAsync(DateTime cutoff);
    }
}
