using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace GestorInventario.Infraestructure.Repositories
{
    public class ProductoRepository:IProductoRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IGestorArchivos _gestorArchivos;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<ProductoRepository> _logger;
        public ProductoRepository(GestorInventarioContext context, IGestorArchivos gestorArchivos, IHttpContextAccessor contextAccessor, ILogger<ProductoRepository> logger)
        {
            _context = context;
            _gestorArchivos = gestorArchivos;
            _contextAccessor = contextAccessor;
            _logger = logger;
        }
        public  IQueryable<Producto> ObtenerTodoProducto()
        {
            var productos = from p in  _context.Productos.Include(x => x.IdProveedorNavigation)
                            select p;
            return productos;
        }
        public async Task<List<Producto>> ObtenerTodos()
        {
            var productos = await _context.Productos.ToListAsync();
            return productos;
        }
        public async Task<Producto> CrearProducto(ProductosViewModel model)
        {
            try
            {
                var producto = new Producto()
                {
                    NombreProducto = model.NombreProducto,
                    Descripcion = model.Descripcion,
                    Imagen = "",
                    Cantidad = model.Cantidad,
                    Precio = model.Precio,
                    FechaCreacion = DateTime.Now,
                    IdProveedor = model.IdProveedor,
                };
                if (producto.Imagen != null)
                {
                    //MemoryStream--> guarda en memoria la imagen
                    using (var memoryStream = new MemoryStream())
                    {
                        //Realiza una copia de la imagen
                        await model.Imagen1.CopyToAsync(memoryStream);
                        //La informacion de la imgen se convierte a un array
                        var contenido = memoryStream.ToArray();
                        //Se obtiene el formato de la imagen .png, .jpg etc
                        var extension = Path.GetExtension(model.Imagen1.FileName);
                        //Guarda la imagen en la carpeta imagenes
                        producto.Imagen = await _gestorArchivos.GuardarArchivo(contenido, extension, "imagenes"
                     );
                    }
                }
                _context.AddEntity(producto);
                return producto;

            }
            catch (Exception ex)
            {

                _logger.LogError("Error al crear el producto",ex);
                return null;
            }
           
        }
        public async Task<HistorialProducto> CrearHistorial(int usuarioId, Producto producto)
        {
            try
            {
                var historialProducto = new HistorialProducto()
                {
                    UsuarioId = usuarioId,
                    Fecha = DateTime.Now,
                    Accion = _contextAccessor.HttpContext.Request.Method.ToString(),
                    Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                };

                _context.AddEntity(historialProducto);
                return historialProducto;
            }
            catch (Exception ex)
            {

                _logger.LogError("Error al crear el historial para el producto", ex);
                return null ;
            }
           
        }
        public async Task<DetalleHistorialProducto> CrearDetalleHistorial(HistorialProducto historialProducto, Producto producto)
        {
            try
            {
                var detalleHistorialProducto = new DetalleHistorialProducto
                {
                    HistorialProductoId = historialProducto.Id,
                    ProductoId = producto.Id,
                    Cantidad = producto.Cantidad,
                    NombreProducto = producto.NombreProducto,
                    Precio = producto.Precio
                };

                // Guardar el detalle del historial de productos
                _context.AddEntity(detalleHistorialProducto);
                return detalleHistorialProducto;
            }
            catch (Exception ex)
            {

                _logger.LogError("Error al crear el detalle del producto en el historial", ex);
                return null;
            }
          
        }
        public async Task<List<Proveedore>> ObtenerProveedores()
        {
            var proveedores= await _context.Proveedores.ToListAsync();
            return proveedores;
        }
        public async Task<Producto> EliminarProductoObtencion(int id)
        {
            var producto = await _context.Productos.Include(p => p.IdProveedorNavigation).FirstOrDefaultAsync(m => m.Id == id);
            return producto;
        }
        public async Task<Producto> EliminarProducto(int id)
        {
            var producto = await _context.Productos
                   .Include(p => p.DetallePedidos)
                       .ThenInclude(dp => dp.Pedido)
                   .Include(p => p.IdProveedorNavigation).Include(x => x.DetalleHistorialProductos)
                   .FirstOrDefaultAsync(m => m.Id == id);
            return producto;
        }
    }
}
