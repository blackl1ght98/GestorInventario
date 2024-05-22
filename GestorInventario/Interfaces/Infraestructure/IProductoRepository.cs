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
        Task<Producto> ObtenerPorId(int id);
        Task<ProductosViewModel> ProductoOriginal(Producto producto);
        Task<HistorialProducto> CrearHitorialAccion(int usuarioId);
        Task<DetalleHistorialProducto> EditDetalleHistorialproducto(HistorialProducto historialProducto1, ProductosViewModel productoOriginal);
        Task<Producto> ActualizarProducto(ProductosViewModel model);
        Task<HistorialProducto> CrearHitorialAccionEdit(int usuarioId);
        Task<DetalleHistorialProducto> DetalleHistorialProductoEdit(HistorialProducto historialProducto, Producto producto);
        bool ProductoExist(int Id);
        Task<bool> TryUpdateAndSaveAsync(ProductosViewModel model);
        Task<Carrito> ObtenerCarritoUsuario(int usuarioActual);
        Task<ItemsDelCarrito> ObtenerProductosCarrito(int carrito, int idProducto);
        Task<ItemsDelCarrito> AgregarOActualizarProductoCarrito(int carritoId, int idProducto, int cantidad);
        Task<Producto> DisminuirCantidadProducto(int idProducto, int cantidad);
        Task<IQueryable<HistorialProducto>> ObtenerTodoHistorial();
        Task<List<HistorialProducto>> DescargarPDF();
        Task<HistorialProducto> HistorialProductoPorId(int id);
        Task<HistorialProducto> EliminarHistorialPorId(int id);
        Task<HistorialProducto> EliminarHistorialPorIdDefinitivo(int id);
        Task<List<HistorialProducto>> EliminarTodoHistorial();
    }
}
