using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.product;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IProductoRepository
    {
        IQueryable<Producto> ObtenerTodoProducto();
        Task<Producto> CrearProducto(ProductosViewModel model);      
        Task<List<Proveedore>> ObtenerProveedores();
        Task<(Producto?, string)> ObtenerProductoPorId(int id);
        Task<OperationResult<string>> EliminarProducto(int Id);

        IQueryable<HistorialProducto> ObtenerTodoHistorial();
        Task<HistorialProducto> ObtenerHistorialProductoPorId(int id);

        Task<OperationResult<string>> EliminarHistorialPorId(int Id);
        Task<List<HistorialProducto>> EliminarTodoHistorial();
        Task<OperationResult<string>> EditarProducto(ProductosViewModel model, int usuarioId);
        Task<OperationResult<string>> AgregarProductoAlCarrito(int userId, int idProducto, int cantidad);
    }
}
