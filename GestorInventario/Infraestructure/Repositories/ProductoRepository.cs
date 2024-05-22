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
            _context.DeleteEntity(producto);
            return producto;
        }
        public async Task<Producto> ObtenerPorId(int id)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == id);
            return producto;
        }
        public async Task<ProductosViewModel> ProductoOriginal(Producto producto)
        {
            var productoOriginal = new ProductosViewModel
            {
                Id = producto.Id,
                NombreProducto = producto.NombreProducto,
                Descripcion = producto.Descripcion,
                Cantidad = producto.Cantidad,
                Precio = producto.Precio,
                Imagen = producto.Imagen,
                IdProveedor = producto.IdProveedor
            };
            return productoOriginal;
        }
        public async Task<HistorialProducto> CrearHitorialAccion(int usuarioId)
        {
            var historialProductoPostActualizacion = new HistorialProducto
            {
                Fecha = DateTime.Now,
                UsuarioId = usuarioId,
                Accion = "Antes-PUT",
                Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.AddEntity(historialProductoPostActualizacion);
            return historialProductoPostActualizacion;
        }
        public async Task<DetalleHistorialProducto> EditDetalleHistorialproducto(HistorialProducto historialProducto1, ProductosViewModel productoOriginal)
        {
            var detalleHistorialProductoPostActualizacion = new DetalleHistorialProducto
            {
                HistorialProductoId = historialProducto1.Id,
                ProductoId = productoOriginal.Id,
                Cantidad = productoOriginal.Cantidad,
                NombreProducto = productoOriginal.NombreProducto,
                Precio = productoOriginal.Precio,
            };
            _context.AddEntity(detalleHistorialProductoPostActualizacion);
            return detalleHistorialProductoPostActualizacion;
        }
        public async Task<Producto> ActualizarProducto(ProductosViewModel model)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == model.Id);
            producto.NombreProducto = model.NombreProducto;
            producto.FechaModificacion = DateTime.Now;
            producto.Descripcion = model.Descripcion;
            producto.Cantidad = model.Cantidad;
            producto.Precio = model.Precio;
            producto.IdProveedor = model.IdProveedor;
            if (model.Imagen1 != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await model.Imagen1.CopyToAsync(memoryStream);
                    var contenido = memoryStream.ToArray();
                    var extension = Path.GetExtension(model.Imagen1.FileName);
                    // Borrar la imagen antigua
                    await _gestorArchivos.BorrarArchivo(producto.Imagen, "imagenes");
                    // Guardar la nueva imagen
                    producto.Imagen = await _gestorArchivos.GuardarArchivo(contenido, extension, "imagenes");
                }
            }

            _context.UpdateEntity(producto);
            return producto;
        }
        public async Task<HistorialProducto> CrearHitorialAccionEdit(int usuarioId)
        {
            var historialProducto = new HistorialProducto
            {
                Fecha = DateTime.Now,
                UsuarioId = usuarioId,
                Accion = "Despues-PUT",
                Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.AddEntity(historialProducto);
            return historialProducto;
        }
        public async Task<DetalleHistorialProducto> DetalleHistorialProductoEdit(HistorialProducto historialProducto, Producto producto)
        {
            var detalleHistorialProducto = new DetalleHistorialProducto
            {
                HistorialProductoId = historialProducto.Id,
                ProductoId = producto.Id,
                Cantidad = producto.Cantidad,
                NombreProducto = producto.NombreProducto,
                Precio = producto.Precio,
            };
            _context.AddEntity(detalleHistorialProducto);
            return detalleHistorialProducto;
        }
        public bool ProductoExist(int Id)
        {
            try
            {
                return _context.Productos.Any(e => e.Id == Id);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el producto");
                return false;
            }
        }
        public async Task<bool> TryUpdateAndSaveAsync(ProductosViewModel model)
        {
            try
            {
                _context.Entry(model).Reload();
                _context.Entry(model).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error de concurrencia ");
                return false;
            }
        }
        public async Task<Carrito> ObtenerCarritoUsuario(int usuarioActual)
        {
            var carrito =await _context.Carritos.FirstOrDefaultAsync(c => c.UsuarioId == usuarioActual);
            if (carrito == null)
            {
                carrito = new Carrito
                {
                    UsuarioId = usuarioActual,
                    FechaCreacion = DateTime.Now
                };
                _context.AddEntity(carrito);

            }
            return carrito;
        }
        public async Task<ItemsDelCarrito> ObtenerProductosCarrito(int carrito, int idProducto)
        {
            var itemCarrito = _context.ItemsDelCarritos.FirstOrDefault(i => i.CarritoId == carrito && i.ProductoId == idProducto);

            return itemCarrito;
        }
        public async Task<ItemsDelCarrito> AgregarOActualizarProductoCarrito(int carritoId, int idProducto, int cantidad)
        {
            var itemCarrito = _context.ItemsDelCarritos.FirstOrDefault(i => i.CarritoId == carritoId && i.ProductoId == idProducto);
            if (itemCarrito == null)
            {
                itemCarrito = new ItemsDelCarrito
                {
                    ProductoId = idProducto,
                    Cantidad = cantidad,
                    CarritoId = carritoId
                };
                _context.AddEntity(itemCarrito);
            }
            else
            {
                itemCarrito.Cantidad += cantidad;
                _context.UpdateEntity(itemCarrito);
            }
            
            return itemCarrito;
        }
        public async Task<Producto> DisminuirCantidadProducto(int idProducto, int cantidad)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.Id == idProducto);
            if (producto != null)
            {
                producto.Cantidad -= cantidad;
                _context.UpdateEntity(producto);
               
            }
            return producto;
        }
       public async Task<IQueryable<HistorialProducto>> ObtenerTodoHistorial()
        {
            var historialProductos = from p in _context.HistorialProductos.Include(x => x.DetalleHistorialProductos)
                                     select p;
            return historialProductos;
        }
        public async Task<List<HistorialProducto>> DescargarPDF()
        {
            var historialProductos = await _context.HistorialProductos
                .Include(hp => hp.DetalleHistorialProductos)
            .ThenInclude(dp => dp.Producto)
                .ToListAsync();
            return historialProductos;
        }
        public async Task<HistorialProducto> HistorialProductoPorId(int id)
        {
            var historialProducto = await _context.HistorialProductos
                   .Include(hp => hp.DetalleHistorialProductos)
                       .ThenInclude(dp => dp.Producto)

                   .FirstOrDefaultAsync(hp => hp.Id == id);
            return historialProducto;
        }
        public async Task<HistorialProducto> EliminarHistorialPorId(int id)
        {
            var historialProducto = await _context.HistorialProductos.Include(x => x.DetalleHistorialProductos).FirstOrDefaultAsync(x => x.Id == id);
            return historialProducto;
        }
        public async Task<HistorialProducto> EliminarHistorialPorIdDefinitivo(int id)
        {
            var historialProducto = await _context.HistorialProductos.Include(x => x.DetalleHistorialProductos).FirstOrDefaultAsync(x => x.Id == id);
            if(historialProducto != null)
            {
                _context.DeleteRangeEntity(historialProducto.DetalleHistorialProductos);
                _context.DeleteEntity(historialProducto);
            }
            return historialProducto;
        }
        public async Task<List<HistorialProducto>> EliminarTodoHistorial()
        {
            var historialProductos = await _context.HistorialProductos.Include(x => x.DetalleHistorialProductos).ToListAsync();
            if(historialProductos != null)
            {
                // Eliminar todos los registros
                foreach (var historialProducto in historialProductos)
                {
                    _context.DeleteRangeEntity(historialProducto.DetalleHistorialProductos);
                    _context.DeleteEntity(historialProducto);
                }
            }
            return historialProductos;

        }
    }
      
}
