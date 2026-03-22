using GestorInventario.Application.DTOs.Carrito;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface ICarritoRepository
    {

        Task<OperationResult<Pedido>> ObtenerCarritoUsuario(int userId);
        Task<OperationResult<List<DetallePedido>>> ObtenerItemsDelCarritoUsuario(int pedidoId);
        Task<OperationResult<DetallePedido>> ItemsDelCarrito(int id);
        OperationResult<IQueryable<DetallePedido>> ObtenerItemsConDetalles(int pedidoId);
        Task<OperationResult<List<Monedum>>> ObtenerMoneda();
        Task<OperationResult<Pedido>> CrearCarritoUsuario(int userId);
        Task<OperationResult<string>> Pagar(string moneda, int userId);
        Task<OperationResult<string>> Incremento(int id);
        Task<OperationResult<string>> Decremento(int id);
        Task<OperationResult<string>> EliminarProductoCarrito(int id);
        Task EliminarCarritoAsync(int carritoId);
       

    }
}
