
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface ICarritoRepository
    {
        //Consultas
        Task<Pedido> ObtenerCarritoUsuario(int userId);
        Task<List<DetallePedido>> ObtenerItemsDelCarritoUsuario(int pedidoId);
        Task<DetallePedido> ItemsDelCarrito(int id);
        IQueryable<DetallePedido> ObtenerItemsConDetalles(int pedidoId);
        Task<List<Monedum>> ObtenerMoneda();       
        Task<List<Pedido>> ObtenerCarritosActivosAsync(int userId);
    }
}
