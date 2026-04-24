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
        Task<Producto> ObtenerProductoPorId(int id);
        Task<Producto> ObtenerProductoPorIdAsync(int productoId);
        Task<bool> ExisteProductoAsync(string nombre);
        Task<OperationResult<Producto>> ActualizarProductoAsync(Producto producto);
      
        Task<DetallePedido?> ObtenerDetallesCarrito(int idCarrito, int idProducto);
        Task<Producto> ObtenerProductoCompletoAsync(int Id);
        Task<OperationResult<Producto>> EliminarProductoAsync(Producto producto);
    }
}
