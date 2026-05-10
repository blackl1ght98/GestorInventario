using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;


namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IProductoRepository
    {
        //Consultas
        IQueryable<Producto> ObtenerTodosLosProductos();
        Task<List<Proveedore>> ObtenerProveedores();
        Task<Producto> ObtenerProductoPorIdAsync(int id);
        Task<bool> ObtenerCodigoUPC(string code);
        Task<DetallePedido> ObtenerDetallesCarrito(int idCarrito, int idProducto);
        Task<Producto> ObtenerProductoCompletoAsync(int Id);
        Task<bool> ExisteNombreProductoAsync(string nombre);
        //Operaciones
        Task<OperationResult<Producto>> AgregarProductoAsync(Producto producto);
        Task<OperationResult<Producto>> ActualizarProductoAsync(Producto producto);
        Task<OperationResult<Producto>> EliminarProductoAsync(Producto producto);
      
    }
}
