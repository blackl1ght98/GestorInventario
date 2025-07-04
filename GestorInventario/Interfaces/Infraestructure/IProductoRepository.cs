using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels.product;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IProductoRepository
    {
        IQueryable<Producto> ObtenerTodoProducto();

        Task<Producto> CrearProducto(ProductosViewModel model);
      
        Task<List<Proveedore>> ObtenerProveedores();
        Task<Producto> EliminarProductoObtencion(int id);
        Task<(bool, string)> EliminarProducto(int Id);
        Task<Producto> ObtenerPorId(int id);
        
        Task<IQueryable<HistorialProducto>> ObtenerTodoHistorial();
        Task<HistorialProducto> HistorialProductoPorId(int id);
        Task<HistorialProducto> EliminarHistorialPorId(int id);
        Task<(bool, string)> EliminarHistorialPorIdDefinitivo(int Id);
        Task<List<HistorialProducto>> EliminarTodoHistorial();
        Task<(bool, string)> EditarProducto(ProductosViewModel model, int usuarioId);
        Task<(bool, string)> AgregarProductosCarrito(int idProducto, int cantidad, int usuarioId);
      
    }
}
