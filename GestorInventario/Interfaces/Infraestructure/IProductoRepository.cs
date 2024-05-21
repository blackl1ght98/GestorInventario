using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IProductoRepository
    {
        IQueryable<Producto> ObtenerTodoProducto();
        Task<List<Producto>> ObtenerTodos();
        Task<Producto> CrearProducto(ProductosViewModel model);
        Task<HistorialProducto> CrearHistorial(int usuarioId, Producto producto);
        Task<DetalleHistorialProducto> CrearDetalleHistorial(HistorialProducto historialProducto, Producto producto);
        Task<List<Proveedore>> ObtenerProveedores();
        Task<Producto> EliminarProductoObtencion(int id);
        Task<Producto> EliminarProducto(int id);
    }
}
