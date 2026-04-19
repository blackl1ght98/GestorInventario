using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.product;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IProductoRepository
    {
        IQueryable<Producto> ObtenerTodosLosProductos();
     
        Task<OperationResult<Producto>> AgregarProductoAsync(Producto producto);
        Task<List<Proveedore>> ObtenerProveedores();
        Task<(Producto?, string)> ObtenerProductoPorId(int id);
   
        Task<bool> ExisteProductoAsync(string nombre);
        Task<OperationResult<Producto>> ActualizarProductoAsync(Producto producto);
      
        Task<OperationResult<DetallePedido>> ActualizarDetallePedidoAsync(DetallePedido pedido);
        Task<OperationResult<DetallePedido>> AgregarDetallePedidoAsync(DetallePedido pedido);
        Task<DetallePedido?> ObtenerDetallesCarrito(int idCarrito, int idProducto);
        Task<Producto> ObtenerProductoCompletoAsync(int Id);
        Task<OperationResult<Producto>> EliminarProductoAsync(Producto producto);
    }
}
