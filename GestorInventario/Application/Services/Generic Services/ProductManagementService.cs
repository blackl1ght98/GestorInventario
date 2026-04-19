using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.ViewModels.product;


namespace GestorInventario.Application.Services.Generic_Services
{
    public class ProductManagementService: IProductManagementService
    {
       
        private readonly IGestorArchivos _gestorArchivos;
        private readonly IBarCodeService _barCodeService;
        private readonly ILogger<ProductManagementService> _logger;
        private readonly IProductoRepository _productoRepository;
        private readonly IImageOptimizerService _imageOptimizerService;
        private readonly ICarritoRepository _carritoRepository;
        public ProductManagementService( IGestorArchivos gestorArchivos, IBarCodeService barcode, ILogger<ProductManagementService> logger,
            IProductoRepository producto, IImageOptimizerService image, ICarritoRepository carrito)
        {
          
            _gestorArchivos = gestorArchivos;
            _barCodeService = barcode;
            _logger = logger;
            _productoRepository = producto;
            _imageOptimizerService = image;
            _carritoRepository = carrito;
        }

        public async Task<OperationResult<Producto>> CrearProducto(ProductosViewModel model)
        {
            var barcodeResult = await _barCodeService.GenerateUniqueBarCodeAsync(BarcodeType.UPC_A, "", true);

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

            if (model.ArchivoImagen != null && model.ArchivoImagen.Length > 0)
            {
                try
                {
                    producto.Imagen = await _imageOptimizerService.OptimizeAndSaveImage(
                        model.ArchivoImagen, "imagenes");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar la imagen: {FileName}", model.ArchivoImagen.FileName);
                    throw new InvalidOperationException("Error al procesar la imagen.", ex);
                }
            }

            var existeProducto = await _productoRepository.ExisteProductoAsync(producto.NombreProducto);
            if (existeProducto)
                return OperationResult<Producto>.Fail("Ya hay un producto con ese nombre, proporcione otro nombre");

            await _productoRepository.AgregarProductoAsync(producto);
            _logger.LogInformation("Producto creado exitosamente: {NombreProducto}, UpcCode: {UpcCode}",
                producto.NombreProducto, producto.UpcCode);

            return OperationResult<Producto>.Ok("", producto);
        }
        public async Task<OperationResult<string>> EditarProducto(ProductosViewModel model, int usuarioId)
        {
            var (producto, mensaje) = await _productoRepository.ObtenerProductoPorId(model.Id);
            if (producto == null)
                return OperationResult<string>.Fail("Producto no encontrado");

            producto.NombreProducto = model.NombreProducto;
            producto.FechaModificacion = DateTime.UtcNow;
            producto.Descripcion = model.Descripcion;
            producto.Cantidad = model.Cantidad;
            producto.Precio = model.Precio;
            producto.IdProveedor = model.IdProveedor;

            string imagenAnterior = producto.Imagen;

            if (model.ArchivoImagen != null && model.ArchivoImagen.Length > 0)
            {
                _logger.LogDebug("Procesando imagen: {FileName}", model.ArchivoImagen.FileName);
                try
                {
                    // 1. Primero guarda la nueva imagen
                    producto.Imagen = await _imageOptimizerService.OptimizeAndSaveImage(
                        model.ArchivoImagen, "imagenes");

                    _logger.LogInformation("Imagen guardada: {ImagenPath}", producto.Imagen);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al procesar la imagen: {FileName}", model.ArchivoImagen.FileName);
                    throw new InvalidOperationException("Error al procesar la imagen.", ex);
                }
            }

            // 2. Actualiza en BD
            var resultado = await _productoRepository.ActualizarProductoAsync(producto);

            // 3. Solo borra la imagen antigua si la BD confirmó el cambio
            if (resultado.Success && !string.IsNullOrEmpty(imagenAnterior))
            {
                string fileName = Path.GetFileName(imagenAnterior);
                await _gestorArchivos.BorrarArchivo(fileName, "imagenes");
            }

            _logger.LogInformation("Producto con ID {Id} actualizado por usuario {UsuarioId}",
                model.Id, usuarioId);
            return OperationResult<string>.Ok("Producto editado con exito");
        }
        public async Task<OperationResult<string>> AgregarProductoAlCarrito(int idProducto, int cantidad, int usuarioId)
        {
           
                // Validar cantidad
                if (cantidad <= 0)
                {
                    return OperationResult<string>.Fail("La cantidad debe ser mayor a cero.");
                }

                // Validar existencia del producto y stock
                var (producto,mensaje) = await _productoRepository.ObtenerProductoPorId(idProducto);
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
                var detalleExistente = await _productoRepository.ObtenerDetallesCarrito(carrito.Data.Id, idProducto);
                    if (detalleExistente != null)
                    {
                        // Sumar la cantidad al ítem existente
                        detalleExistente.Cantidad += cantidad;
                        await _productoRepository.ActualizarDetallePedidoAsync(detalleExistente);
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
                        await _productoRepository.AgregarDetallePedidoAsync(detalle);
                    }

                }

                // Actualizar el inventario del producto
                producto.Cantidad -= cantidad;
                await _productoRepository.ActualizarProductoAsync(producto);
                return OperationResult<string>.Ok("Producto agregado con exito");
          

        }
        public async Task<OperationResult<string>> EliminarProducto(int Id)
        {
            var producto = await _productoRepository.ObtenerProductoCompletoAsync(Id);
            if (producto == null)
                return OperationResult<string>.Fail("No hay productos para eliminar");

            // 1. Elimina en BD primero
            var resultado = await _productoRepository.EliminarProductoAsync(producto);

            // 2. Solo borra la imagen si la BD confirmó
            if (resultado.Success && !string.IsNullOrEmpty(producto.Imagen))
            {
                string fileName = Path.GetFileName(producto.Imagen);
                await _gestorArchivos.BorrarArchivo(fileName, "imagenes");
            }

            _logger.LogInformation("Producto con ID {Id} eliminado correctamente.", Id);
            return OperationResult<string>.Ok("Producto eliminado con exito");
        }

    }
}
