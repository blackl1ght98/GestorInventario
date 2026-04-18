using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.product;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IGestorArchivos _gestorArchivos;        
        private readonly ILogger<ProductoRepository> _logger;   
        private readonly IBarCodeService _barCodeService;
        private readonly ICarritoRepository _carritoRepository;
        private readonly IImageOptimizerService _imageOptimizerService;      
        private readonly ICurrentUserAccessor _currentUserAccessor;
        public ProductoRepository(GestorInventarioContext context, IGestorArchivos gestorArchivos,  ICurrentUserAccessor current,
        ILogger<ProductoRepository> logger,  ICarritoRepository carrito, IBarCodeService code, IImageOptimizerService optimizer)
        {
            _context = context;
            _gestorArchivos = gestorArchivos;          
            _logger = logger;
            _barCodeService = code;
            _carritoRepository=carrito;
            _imageOptimizerService=optimizer;         
            _currentUserAccessor = current;
        }    
     
        public IQueryable<Producto> ObtenerTodosLosProductos()=>from p in _context.Productos.Include(x => x.IdProveedorNavigation)orderby p.Id  select p;
        public async Task<List<Producto>> ObtenerProductos() => await _context.Productos.ToListAsync();
        public async Task<OperationResult<Producto>> CrearProducto(ProductosViewModel model)
        {


            return await _context.ExecuteInTransactionAsync(async () =>
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
                if (model.ArchivoImagen != null && model.ArchivoImagen.Length > 0)
                {
                    _logger.LogDebug("Procesando imagen: {FileName}, Tamaño: {Length} bytes", model.ArchivoImagen.FileName, model.ArchivoImagen.Length);
                    try
                    {
                        using var memoryStream = new MemoryStream();
                        await model.ArchivoImagen.CopyToAsync(memoryStream);
                        var contenido = memoryStream.ToArray();
                        var extension = Path.GetExtension(model.ArchivoImagen.FileName)?.ToLowerInvariant();
                        if (string.IsNullOrEmpty(extension))
                        {
                            _logger.LogWarning("No se pudo determinar la extensión del archivo: {FileName}", model.ArchivoImagen.FileName);
                            throw new InvalidOperationException("No se pudo determinar la extensión del archivo.");

                        }
                        producto.Imagen = await _gestorArchivos.GuardarArchivo(contenido, extension, "imagenes");
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "Error al procesar la imagen: {FileName}", model.ArchivoImagen.FileName);
                        throw new InvalidOperationException("Error al procesar la imagen.", ex);
                    }
                }

                // Guardar producto
                _logger.LogDebug("Agregando producto a la base de datos: {NombreProducto}", producto.NombreProducto);
                var existeProducto = await _context.Productos.FirstOrDefaultAsync(x => x.NombreProducto == producto.NombreProducto);
                if (existeProducto != null)
                {
                    return OperationResult<Producto>.Fail("Ya hay un producto con ese nombre, proporcione otro nombre");
                }
                await _context.AddEntityAsync(producto);
                _logger.LogInformation("Producto creado exitosamente: {NombreProducto}, UpcCode: {UpcCode}", producto.NombreProducto, producto.UpcCode);
                return OperationResult<Producto>.Ok("", producto);
            });
           
        }
     
         
        public async Task<List<Proveedore>> ObtenerProveedores()=>await _context.Proveedores.ToListAsync();
        public async Task<(Producto?,string)> ObtenerProductoPorId(int id)
        {
            var producto = await _context.Productos.Include(p => p.IdProveedorNavigation).FirstOrDefaultAsync(m => m.Id == id);
            return producto is null ? (null,"Producto no encontrado"): (producto,"Producto encontrado");
        }
       
        public async Task<OperationResult<string>> EliminarProducto(int Id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var producto = await _context.Productos
                    .Include(p => p.DetallePedidos)
                    .ThenInclude(dp => dp.Pedido)
                    .Include(p => p.IdProveedorNavigation)
                    .FirstOrDefaultAsync(m => m.Id == Id);

                if (producto == null)
                {
                    return OperationResult<string>.Fail("No hay productos para eliminar");
                }

                if (!string.IsNullOrEmpty(producto.Imagen))
                {
                    // Extraer el nombre del archivo de la URL
                    string fileName = Path.GetFileName(producto.Imagen);
                    await _gestorArchivos.BorrarArchivo(fileName, "imagenes");
                }

                await _context.DeleteEntityAsync(producto);         
                _logger.LogInformation($"Producto con ID {Id} eliminado correctamente.");
                return OperationResult<string>.Ok("Producto eliminado con exito");
            });
            
        }
             
      
        public async Task<OperationResult<string>> EditarProducto(ProductosViewModel model, int usuarioId)
        {


            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (producto == null)
                {
                    return OperationResult<string>.Fail("Producto no encontrado");
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


                // Actualizar el producto y crear historial después
                await ActualizarProductoYCrearHistorialAsync(producto, model, usuarioId);
                await _context.SaveChangesAsync();
             
                _logger.LogInformation($"Producto con ID {model.Id} actualizado correctamente por el usuario {usuarioId}.");
                return OperationResult<string>.Ok("Producto editado con exito");
            });
           
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
            if (model.ArchivoImagen != null && model.ArchivoImagen.Length > 0)
            {
                _logger.LogDebug("Procesando imagen: {FileName}", model.ArchivoImagen.FileName);

                try
                {
                    if (!string.IsNullOrEmpty(producto.Imagen))
                    {
                        // Extraer el nombre del archivo de la URL
                        string fileName = Path.GetFileName(producto.Imagen);
                        await _gestorArchivos.BorrarArchivo(fileName, "imagenes");
                    }
                    // Guardar imagen optimizada (sin parámetros en la URL)
                    producto.Imagen = await _imageOptimizerService.OptimizeAndSaveImage(model.ArchivoImagen, "imagenes");

                    _logger.LogInformation("Imagen guardada: {ImagenPath}", producto.Imagen);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar la imagen: {FileName}", model.ArchivoImagen.FileName);
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

           
        }
 

        public async Task<OperationResult<string>> AgregarProductoAlCarrito(int idProducto, int cantidad, int usuarioId)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                // Validar cantidad
                if (cantidad <= 0)
                {
                    return OperationResult<string>.Fail("La cantidad debe ser mayor a cero.");
                }

                // Validar existencia del producto y stock
                var producto = await _context.Productos.FirstOrDefaultAsync(x => x.Id == idProducto);
                if (producto == null)
                {
                    return OperationResult<string>.Fail("El producto no existe.");
                }
                if (producto.Cantidad < cantidad)
                {
                    return OperationResult<string>.Fail("No hay suficientes productos en stock.");
                }
                // Obtener o crear el carrito
                var carrito = await _carritoRepository.CrearCarritoUsuario(usuarioId);
                if (carrito != null)
                {
                    var detalleExistente = await _context.DetallePedidos
                  .FirstOrDefaultAsync(d => d.PedidoId == carrito.Data.Id && d.ProductoId == idProducto);
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
                            PedidoId = carrito.Data.Id,
                            ProductoId = idProducto,
                            Cantidad = cantidad
                        };
                        await _context.AddEntityAsync(detalle);
                    }

                }

                // Actualizar el inventario del producto
                producto.Cantidad -= cantidad;
                await _context.UpdateEntityAsync(producto);
                return OperationResult<string>.Ok("Producto agregado con exito");
            });
           
        }
    }
}
