using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface ICarritoRepository
    {

        Task<OperationResult<Pedido>> ObtenerCarritoUsuario(int userId);
        Task<List<DetallePedido>> ObtenerItemsDelCarritoUsuario(int pedidoId);
        Task<OperationResult<DetallePedido>> ItemsDelCarrito(int id);
        IQueryable<DetallePedido> ObtenerItemsConDetalles(int pedidoId);
        Task<List<Monedum>> ObtenerMoneda();
        Task<Pedido?> CrearCarritoUsuario(int userId);
        Task<OperationResult<string>> PagarV2(string moneda, int userId);
        Task<OperationResult<string>> Incremento(int id);
        Task<OperationResult<string>> Decremento(int id);
        Task<OperationResult<string>> EliminarProductoCarrito(int id);

    }
}
