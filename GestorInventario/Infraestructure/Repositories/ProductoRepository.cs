
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Application;
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
        private readonly IBarCodeService _barCodeService;
        private readonly ICarritoRepository _carritoRepository;
        private readonly ImageOptimizerService _imageOptimizerService;
      
        public ProductoRepository(GestorInventarioContext context, IGestorArchivos gestorArchivos, IHttpContextAccessor contextAccessor,
        ILogger<ProductoRepository> logger,  ICarritoRepository carrito, IBarCodeService code, ImageOptimizerService optimizer)
        {
            _context = context;
            _gestorArchivos = gestorArchivos;
            _contextAccessor = contextAccessor;
            _logger = logger;
            _barCodeService = code;
            _carritoRepository=carrito;
            _imageOptimizerService=optimizer;
        }    
     
        public IQueryable<Producto> ObtenerTodoProducto()=>from p in _context.Productos.Include(x => x.IdProveedorNavigation)orderby p.Id  select p;

        public async Task<Producto> CrearProducto(ProductosViewModel model)
        {
            if (model == null)
            {
                _logger.LogError("El modelo ProductosViewModel es nulo.");
                throw new ArgumentNullException(nameof(model));
            }

            _logger.LogInformation("Iniciando creación de producto: {NombreProducto}", model.NombreProducto);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var barcodeResult = await _barCodeService.GenerateUniqueBarCodeAsync(BarcodeType.UPC_A, "", true); 

                // Crear entidad Producto
                var producto = new Producto
                {
                    NombreProducto = model.NombreProducto,
                    Descripcion = model.Descripcion,
                    Imagen = "",
                    Cantidad = model.Cantidad,
                    Precio = model.Precio,
                    FechaCreacion = DateTime.Now,
                    FechaModificacion = DateTime.Now,
                    IdProveedor = model.IdProveedor,
                    UpcCode = barcodeResult.Code
                };

                // Procesar imagen si existe
                if (model.Imagen1 != null && model.Imagen1.Length > 0)
                {
                    _logger.LogDebug("Procesando imagen: {FileName}, Tamaño: {Length} bytes", model.Imagen1.FileName, model.Imagen1.Length);
                    try
                    {
                        using var memoryStream = new MemoryStream();
                        await model.Imagen1.CopyToAsync(memoryStream);
                        var contenido = memoryStream.ToArray();
                        var extension = Path.GetExtension(model.Imagen1.FileName)?.ToLowerInvariant();
                        if (string.IsNullOrEmpty(extension))
                        {
                            _logger.LogWarning("No se pudo determinar la extensión del archivo: {FileName}", model.Imagen1.FileName);
                            throw new InvalidOperationException("No se pudo determinar la extensión del archivo.");
                            
                        }
                        producto.Imagen = await _gestorArchivos.GuardarArchivo(contenido, extension, "imagenes");
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Error al procesar la imagen: {FileName}", model.Imagen1.FileName);
                        throw new InvalidOperationException("Error al procesar la imagen.", ex);
                    }
                }

                // Guardar producto
                _logger.LogDebug("Agregando producto a la base de datos: {NombreProducto}", producto.NombreProducto);
                await _context.AddEntityAsync(producto);
                await CrearHistorial(producto);
              
            await transaction.CommitAsync();

                _logger.LogInformation("Producto creado exitosamente: {NombreProducto}, UpcCode: {UpcCode}", producto.NombreProducto, producto.UpcCode);
                return producto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el producto: {NombreProducto}", model.NombreProducto);
                await transaction.RollbackAsync();
                throw;
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
             
                _logger.LogError(ex,"Error al crear el historial");
            }
        }
         
        public async Task<List<Proveedore>> ObtenerProveedores()=>await _context.Proveedores.ToListAsync();
        public async Task<(Producto?,string)> ObtenerProductoPorId(int id)
        {
            var producto = await _context.Productos.Include(p => p.IdProveedorNavigation).FirstOrDefaultAsync(m => m.Id == id);
            return producto is null ? (null,"Producto no encontrado"): (producto,"Producto encontrado");
        }
        public async Task<IQueryable<HistorialProducto>> ObtenerTodoHistorial() => from p in _context.HistorialProductos.Include(x => x.DetalleHistorialProductos) select p;
        public async Task<HistorialProducto> ObtenerHistorialProductoPorId(int id) => await _context.HistorialProductos.Include(hp => hp.DetalleHistorialProductos).FirstOrDefaultAsync(hp => hp.Id == id);
        public async Task<(bool, string)> EliminarProducto(int Id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var producto = await _context.Productos
                    .Include(p => p.DetallePedidos)
                    .ThenInclude(dp => dp.Pedido)
                    .Include(p => p.IdProveedorNavigation)
                    .FirstOrDefaultAsync(m => m.Id == Id);

                if (producto == null)
                {
                    return (false, "No hay productos para eliminar");
                }

                if (!string.IsNullOrEmpty(producto.Imagen))
                {
                    // Extraer el nombre del archivo de la URL
                    string fileName = Path.GetFileName(producto.Imagen); 
                    await _gestorArchivos.BorrarArchivo(fileName, "imagenes");
                }

                await _context.DeleteEntityAsync(producto);
                await _context.SaveChangesAsync();
               
                await transaction.CommitAsync();
                _logger.LogInformation($"Producto con ID {Id} eliminado correctamente.");
                return (true, "Producto eliminado con exito");
            }
            catch (Exception ex)
            {
               await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error al eliminar el producto con ID {Id}");
                return (false, "Ocurrió un error inesperado");
            }
        }
       
          
                       
        public async Task<(bool, string)> EliminarHistorialPorId(int Id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
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

                _logger.LogError(ex,"Error al eliminar el historial del producto" );
                await transaction.RollbackAsync();
                return (false, "Ocurrio un error inesperado al eliminar el historial del producto");
            }
           
        }
        public async Task<List<HistorialProducto>> EliminarTodoHistorial()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
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
                _logger.LogError(ex,"Error al eliminar el historial del producto");
                await transaction.RollbackAsync();
                return null;
            }
           

        }
        public async Task<(bool, string)> EditarProducto(ProductosViewModel model, int usuarioId)
        {
            if (model == null || model.Id <= 0 || string.IsNullOrEmpty(model.NombreProducto))
            {
                return (false, "Modelo inválido");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (producto == null)
                {
                    return (false, "Producto no encontrado");
                }
                // Crear historial antes de la actualización
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
                await CrearHistorialProductoAsync(productoOriginal, usuarioId, "Antes-PUT");

                // Actualizar el producto y crear historial después
                await ActualizarProductoYCrearHistorialAsync(producto, model, usuarioId);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation($"Producto con ID {model.Id} actualizado correctamente por el usuario {usuarioId}.");
                return (true, "Producto editado con exito");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, $"Error de concurrencia al actualizar el producto con ID {model.Id}");

                // Reintento único (como en el original)
                var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (producto == null)
                {
                    return (false, "Producto no encontrado");
                }

                _context.Entry(producto).Reload();

                // Crear historial antes del reintento
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
                await CrearHistorialProductoAsync(productoOriginal, usuarioId, "Antes-PUT (Reintento)");

                // Actualizar nuevamente y crear historial después
                await ActualizarProductoYCrearHistorialAsync(producto, model, usuarioId, true);

                using var retryTransaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await retryTransaction.CommitAsync();
                    _logger.LogInformation($"Producto con ID {model.Id} actualizado tras reintento de concurrencia.");
                    return (true, "Producto editado con exito");
                }
                catch (Exception retryEx)
                {
                    await retryTransaction.RollbackAsync();
                    _logger.LogError(retryEx, $"Error al reintentar actualizar el producto con ID {model.Id}");
                    return (false, "Error al actualizar tras reintento de concurrencia");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error al actualizar el producto con ID {model.Id}");
                return (false, "Error al actualizar");
            }
        }

        private async Task ActualizarProductoYCrearHistorialAsync(Producto producto, ProductosViewModel model, int usuarioId, bool isRetry = false)
        {
           
            producto.NombreProducto = model.NombreProducto;
            producto.FechaModificacion = DateTime.UtcNow;
            producto.Descripcion = model.Descripcion;
            producto.Cantidad = model.Cantidad;
            producto.Precio = model.Precio;
            producto.IdProveedor = model.IdProveedor;

            // Procesar imagen si existe - CON OPTIMIZACIÓN
            if (model.Imagen1 != null && model.Imagen1.Length > 0)
            {
                _logger.LogDebug("Procesando imagen: {FileName}", model.Imagen1.FileName);

                try
                {
                    if (!string.IsNullOrEmpty(producto.Imagen))
                    {
                        // Extraer el nombre del archivo de la URL
                        string fileName = Path.GetFileName(producto.Imagen);
                        await _gestorArchivos.BorrarArchivo(fileName, "imagenes");
                    }
                    // Guardar imagen optimizada (sin parámetros en la URL)
                    producto.Imagen = await _imageOptimizerService.OptimizeAndSaveImage(model.Imagen1, "imagenes");

                    _logger.LogInformation("Imagen guardada: {ImagenPath}", producto.Imagen);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar la imagen: {FileName}", model.Imagen1.FileName);
                    throw new InvalidOperationException("Error al procesar la imagen.", ex);
                }
            }

            await _context.UpdateEntityAsync(producto);

            // Crear historial después de la actualización
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

            await CrearHistorialProductoAsync(productoActualizado, usuarioId, isRetry ? "Despues-PUT (Reintento)" : "Despues-PUT");
        }
        private async Task CrearHistorialProductoAsync(ProductosViewModel producto, int usuarioId, string accion)
        {
            try
            {
                var historialProducto = new HistorialProducto
                {
                    Fecha = DateTime.UtcNow,
                    UsuarioId = usuarioId,
                    Accion = accion,
                    Ip = _contextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Desconocido"
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear el historial para el producto ID {producto.Id}");
                throw; 
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
                return (true, "Producto agregado con exito");
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
