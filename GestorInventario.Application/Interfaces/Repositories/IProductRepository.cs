using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Repositories
{
    public interface IProductRepository
    {
        //Consultas
        IQueryable<Productos> ObtenerTodosLosProductos();
        Task<List<Proveedores>> ObtenerProveedores();
        Task<Productos> ObtenerProductoPorIdAsync(int id);
        Task<bool> ObtenerCodigoUPC(string code);
        Task<DetallePedido> ObtenerDetallesCarrito(int idCarrito, int idProducto);
        Task<Productos> ObtenerProductoCompletoAsync(int Id);
        Task<bool> ExisteNombreProductoAsync(string nombre);
        //Operaciones
        Task<OperationResult<Productos>> AgregarProductoAsync(Productos producto);
        Task<OperationResult<Productos>> ActualizarProductoAsync(Productos producto);
        Task<OperationResult<Productos>> EliminarProductoAsync(Productos producto);
      
    }
}
