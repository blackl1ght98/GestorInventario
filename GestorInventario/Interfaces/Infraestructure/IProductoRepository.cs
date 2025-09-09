using GestorInventario.Domain.Models;
using GestorInventario.ViewModels.product;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IProductoRepository
    {
        IQueryable<Producto> ObtenerTodoProducto();
        Task<Producto> CrearProducto(ProductosViewModel model);      
        Task<List<Proveedore>> ObtenerProveedores();
        Task<(Producto?, string)> ObtenerProductoPorId(int id);
        Task<(bool, string)> EliminarProducto(int Id);
             
        Task<IQueryable<HistorialProducto>> ObtenerTodoHistorial();
        Task<HistorialProducto> ObtenerHistorialProductoPorId(int id);
        
        Task<(bool, string)> EliminarHistorialPorId(int Id);
        Task<List<HistorialProducto>> EliminarTodoHistorial();
        Task<(bool, string)> EditarProducto(ProductosViewModel model, int usuarioId);       
        Task<(bool, string)> AgregarProductoAlCarrito(int userId, int idProducto, int cantidad);
    }
}
