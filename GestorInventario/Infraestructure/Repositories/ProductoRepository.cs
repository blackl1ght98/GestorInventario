﻿using GestorInventario.Domain.Models;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.product;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace GestorInventario.Infraestructure.Repositories
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IGestorArchivos _gestorArchivos;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<ProductoRepository> _logger;   
        private readonly IConfiguration _configuration; 
        private readonly ICarritoRepository _carritoRepository;
        public ProductoRepository(GestorInventarioContext context, IGestorArchivos gestorArchivos, IHttpContextAccessor contextAccessor,
        ILogger<ProductoRepository> logger, IConfiguration configuration, ICarritoRepository carrito)
        {
            _context = context;
            _gestorArchivos = gestorArchivos;
            _contextAccessor = contextAccessor;
            _logger = logger;      
            _configuration = configuration;
            _carritoRepository=carrito;
        }    
     
        public IQueryable<Producto> ObtenerTodoProducto()=>from p in _context.Productos.Include(x => x.IdProveedorNavigation)orderby p.Id  select p;             
      
        public async Task<Producto> CrearProducto(ProductosViewModel model)
        {
            using var transaction= await _context.Database.BeginTransactionAsync();
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
                            producto.Imagen = await _gestorArchivos.GuardarArchivo(contenido, extension, "imagenes");
                            
                    }
                }
               await _context.AddEntityAsync(producto);
                await CrearHistorial(producto);
                await transaction.CommitAsync();
                return producto;

            }
            catch (Exception ex)
            {

                _logger.LogError("Error al crear el producto", ex);
                await transaction.RollbackAsync();
                return null;
            }

        }   
        private async Task CrearHistorial(Producto producto)
        {
         
            try
            {
                var existeUsuario = _contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var historialProducto = new HistorialProducto()
                    {
                        UsuarioId = usuarioId,
                        Fecha = DateTime.Now,
                        Accion = _contextAccessor.HttpContext.Request.Method.ToString(),
                        Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                    };

                    await _context.AddEntityAsync(historialProducto);
                    var detalleHistorialProducto = new DetalleHistorialProducto
                    {
                        HistorialProductoId = historialProducto.Id,
                        Cantidad = producto.Cantidad,
                        NombreProducto = producto.NombreProducto,
                        Descripcion = producto.Descripcion,
                        Precio = producto.Precio
                    };
                    await _context.AddEntityAsync(detalleHistorialProducto);
               
                }
               
            }
            catch (Exception ex)
            {

                _logger.LogError("Error al crear el historial");
            }
        }
         
        public async Task<List<Proveedore>> ObtenerProveedores()=>await _context.Proveedores.ToListAsync();      
        public async Task<Producto> EliminarProductoObtencion(int id)=>await _context.Productos.Include(p => p.IdProveedorNavigation).FirstOrDefaultAsync(m => m.Id == id);           
        public async Task<(bool, string)> EliminarProducto(int Id)
        {
            using var transaction=  await _context.Database.BeginTransactionAsync();
            try
            {
                var producto = await _context.Productos.Include(p => p.DetallePedidos).ThenInclude(dp => dp.Pedido).Include(p => p.IdProveedorNavigation).FirstOrDefaultAsync(m => m.Id == Id);
                if (producto == null)
                {
                    return (false, "No hay productos para eliminar");
                }
               
               await _context.DeleteEntityAsync(producto);
               await transaction.CommitAsync();
               return (true, null);

            }
            catch (Exception ex)
            {

                _logger.LogError("Error al eliminar el producto", ex);
                await transaction.RollbackAsync();
                return (false,"Ocurrio un error inesperado");
            }
              
        }      
        public async Task<Producto> ObtenerPorId(int id)=>await _context.Productos.FirstOrDefaultAsync(x => x.Id == id);
        public async Task<IQueryable<HistorialProducto>> ObtenerTodoHistorial()=>from p in _context.HistorialProductos.Include(x => x.DetalleHistorialProductos) select p;      
        public async Task<HistorialProducto> HistorialProductoPorId(int id)=>await _context.HistorialProductos.Include(hp => hp.DetalleHistorialProductos).FirstOrDefaultAsync(hp => hp.Id == id);     
        public async Task<HistorialProducto> EliminarHistorialPorId(int id)=>await _context.HistorialProductos.Include(x => x.DetalleHistorialProductos).FirstOrDefaultAsync(x => x.Id == id);               
        public async Task<(bool, string)> EliminarHistorialPorIdDefinitivo(int Id)
        {
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
            {
                var historialProducto = await _context.HistorialProductos.Include(x => x.DetalleHistorialProductos).FirstOrDefaultAsync(x => x.Id == Id);
                if (historialProducto != null)
                {
                    _context.DeleteRangeEntity(historialProducto.DetalleHistorialProductos);
                    _context.DeleteEntity(historialProducto);
                }
                else
                {
                    return (false, "No se puede eliminar, el historial no existe");
                }
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {

                _logger.LogError("Error al eliminar el historial del producto", ex);
                await transaction.RollbackAsync();
                return (false, "Ocurrio un error inesperado");
            }
           
        }
        public async Task<List<HistorialProducto>> EliminarTodoHistorial()
        {
            using var transaction=await _context.Database.BeginTransactionAsync();
            try
            {
                var historialProductos = await _context.HistorialProductos.Include(x => x.DetalleHistorialProductos).ToListAsync();
                if (historialProductos != null)
                {
                    // Eliminar todos los registros
                    foreach (var historialProducto in historialProductos)
                    {
                        _context.DeleteRangeEntity(historialProducto.DetalleHistorialProductos);
                        _context.DeleteEntity(historialProducto);
                    }
                    await transaction.CommitAsync();
                    return historialProductos;
                }
                return null;
            }
            catch (Exception ex)
            {

                _logger.LogError("Error al eliminar el historial del producto", ex);
                await transaction.RollbackAsync();
                return null;
            }
           

        }
        public async Task<(bool, string)> EditarProducto(ProductosViewModel model, int usuarioId)
        {
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
            {
                if (model == null || model.Id <= 0 || string.IsNullOrEmpty(model.NombreProducto))
                {
                    return (false, "Modelo inválido");
                }

                var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (producto == null)
                {
                    return (false, "Producto no encontrado");
                }
              
                await ActualizarProducto(model, producto, usuarioId);
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogCritical("Error de concurrencia en la base de datos", ex);
                var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (producto == null)
                {
                    return (false, "Producto no encontrado");
                }
                _context.ReloadEntity(producto);
                _context.EntityModified(producto);

              
                await ActualizarProducto(model, producto, usuarioId);
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error con la ectualizacion", ex);
                await transaction.RollbackAsync();
                return (false, "Error al actualizar");
            }

        }
        private async Task ActualizarProducto(ProductosViewModel model, Producto producto, int usuarioId)
        {
           
            try
            {
                // Crear el historial antes de la actualización
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
                await CrearHistorialProducto(productoOriginal, usuarioId, "Antes-PUT");

                // Actualizar los datos del producto
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

                        // Borrar la imagen antigua y guardar la nueva
                        await _gestorArchivos.BorrarArchivo(producto.Imagen, "imagenes");
                        producto.Imagen = await _gestorArchivos.GuardarArchivo(contenido, extension, "imagenes");
                    }
                }             
                await _context.UpdateEntityAsync(producto);            
                var productoActualizado = new ProductosViewModel
                {
                    Id = producto.Id,
                    NombreProducto = producto.NombreProducto,
                    Descripcion = producto.Descripcion,
                    Cantidad = producto.Cantidad,
                    Precio = producto.Precio,
                    Imagen = producto.Imagen,
                    IdProveedor = producto.IdProveedor
                };
                await CrearHistorialProducto(productoActualizado, usuarioId, "Despues-PUT");
             
            }
            catch (Exception ex)
            {
                _logger.LogError("Error con la actualización", ex);
               
            }
        }

        private async Task CrearHistorialProducto(ProductosViewModel producto, int usuarioId, string accion)
        {
            
            try
            {
                var historialProducto = new HistorialProducto
                {
                    Fecha = DateTime.Now,
                    UsuarioId = usuarioId,
                    Accion = accion,
                    Ip = _contextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                await _context.AddEntityAsync(historialProducto);

                var detalleHistorialProducto = new DetalleHistorialProducto
                {
                    HistorialProductoId = historialProducto.Id,
                    Cantidad = producto.Cantidad,
                    NombreProducto = producto.NombreProducto,
                    Descripcion = producto.Descripcion,
                    Precio = producto.Precio,
                };
                await _context.AddEntityAsync(detalleHistorialProducto);
               
            }
            catch (Exception ex)
            {

                _logger.LogError("Error al crear el historial", ex);
               
            }
           
        }
       
        public async Task<(bool, string)> AgregarProductoAlCarrito(int idProducto, int cantidad, int usuarioId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validar cantidad
                if (cantidad <= 0)
                {
                    return (false, "La cantidad debe ser mayor a cero.");
                }

                // Validar existencia del producto y stock
                var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == idProducto);
                if (producto == null)
                {
                    return (false, "El producto no existe.");
                }
                if (producto.Cantidad < cantidad)
                {
                    return (false, "No hay suficientes productos en stock.");
                }

                // Obtener o crear el carrito
                var carrito = await _carritoRepository.CrearCarritoUsuario(usuarioId);

                // Verificar si el producto ya está en el carrito
                var detalleExistente = await _context.DetallePedidos
                    .FirstOrDefaultAsync(d => d.PedidoId == carrito.Id && d.ProductoId == idProducto);

                if (detalleExistente != null)
                {
                    // Sumar la cantidad al ítem existente
                    detalleExistente.Cantidad += cantidad;
                    await _context.UpdateEntityAsync(detalleExistente);
                }
                else
                {
                    // Crear un nuevo ítem en el carrito
                    var detalle = new DetallePedido
                    {
                        PedidoId = carrito.Id,
                        ProductoId = idProducto,
                        Cantidad = cantidad
                    };
                    await _context.AddEntityAsync(detalle);
                }

                // Actualizar el inventario del producto
                producto.Cantidad -= cantidad;
                await _context.UpdateEntityAsync(producto);

                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar producto al carrito");
                await transaction.RollbackAsync();
                return (false, "Ocurrió un error inesperado. Por favor, contacte con el administrador o intentelo de nuevo más tarde.");
            }
        }
    }
}
