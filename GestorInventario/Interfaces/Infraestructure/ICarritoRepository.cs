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
        
        Task<List<Pedido>> ObtenerCarritosActivosAsync(int userId);
    
      
   
      
      

    }
}
