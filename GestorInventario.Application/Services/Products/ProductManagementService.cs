using GestorInventario.Domain.enums.Productos;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Common;
using GestorInventario.Interfaces.Application.Services.Products;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Renderer;
using GestorInventario.Shared.DTOS.Products;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;


namespace GestorInventario.Application.Services.Products
{
    public class ProductManagementService: IProductManagementService
    {
       
        private readonly IGestorArchivos _gestorArchivos;
        private readonly IBarCodeService _barCodeService;
        private readonly ILogger<ProductManagementService> _logger;
        private readonly IProductoRepository _productoRepository;
        private readonly IImageOptimizerService _imageOptimizerService;  
      
        public ProductManagementService( IGestorArchivos gestorArchivos, IBarCodeService barcode, ILogger<ProductManagementService> logger,
        IProductoRepository producto, IImageOptimizerService image)
        {         
            _gestorArchivos = gestorArchivos;
            _barCodeService = barcode;
            _logger = logger;
            _productoRepository = producto;
            _imageOptimizerService = image;
        }

        public async Task<OperationResult<Producto>> CrearProducto(ProductoDto model)
        {
            var barcodeResult = await _barCodeService.GenerateUniqueBarCodeAsync(
                BarcodeType.EAN_13, "", true);

            var producto = new Producto
            {
                NombreProducto = model.NombreProducto,
                Descripcion = model.Descripcion,
                Imagen = "",
                Cantidad = model.Cantidad,
                Precio = model.Precio,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow,
                IdProveedor = model.IdProveedor,
                CodigoBarras = barcodeResult.Code,
                TipoCodigoBarras = BarcodeType.EAN_13.ToString(),
                CodigoBarrasImagen = barcodeResult.ImagePath,
            };

            if (model.ArchivoImagenBytes is { Length: > 0 } &&
                !string.IsNullOrEmpty(model.ArchivoImagenNombre))
            {
                try
                {
                    producto.Imagen = await _imageOptimizerService.OptimizeAndSaveImageAsync(
                        model.ArchivoImagenBytes,
                        model.ArchivoImagenNombre);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error al procesar la imagen: {FileName}",
                        model.ArchivoImagenNombre);
                    throw new InvalidOperationException(
                        "Error al procesar la imagen.", ex);
                }
            }

            var existeProducto = await _productoRepository
                .ExisteNombreProductoAsync(producto.NombreProducto);
            if (existeProducto)
                return OperationResult<Producto>.Fail(
                    "Ya hay un producto con ese nombre, proporcione otro nombre");

            await _productoRepository.AgregarProductoAsync(producto);
            _logger.LogInformation(
                "Producto creado exitosamente: {NombreProducto}, UpcCode: {UpcCode}",
                producto.NombreProducto, producto.CodigoBarras);

            return OperationResult<Producto>.Ok("", producto);
        }
        public async Task<OperationResult<string>> EditarProducto(EditarProductoDto model, int usuarioId)
        {
            var producto = await _productoRepository.ObtenerProductoPorIdAsync(model.Id);
            if (producto is null)
                return OperationResult<string>.Fail("Producto no encontrado");

            producto.NombreProducto = model.NombreProducto;
            producto.FechaModificacion = DateTime.UtcNow;
            producto.Descripcion = model.Descripcion;
            producto.Cantidad = model.Cantidad;
            producto.Precio = model.Precio;
            producto.IdProveedor = model.IdProveedor;

            string? imagenAnterior = producto.Imagen;

            if (model.ArchivoImagenBytes is { Length: > 0 } &&
                !string.IsNullOrEmpty(model.ArchivoImagenNombre))
            {
                _logger.LogDebug(
                    "Procesando imagen: {FileName}", model.ArchivoImagenNombre);

                try
                {
                    // 1. Primero guarda la nueva imagen
                    producto.Imagen = await _imageOptimizerService.OptimizeAndSaveImageAsync(
                        model.ArchivoImagenBytes,
                        model.ArchivoImagenNombre,
                        "imagenes");

                    _logger.LogInformation(
                        "Imagen guardada: {ImagenPath}", producto.Imagen);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error al procesar la imagen: {FileName}",
                        model.ArchivoImagenNombre);
                    throw new InvalidOperationException(
                        "Error al procesar la imagen.", ex);
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

            _logger.LogInformation(
                "Producto con ID {Id} actualizado por usuario {UsuarioId}",
                model.Id, usuarioId);

            return OperationResult<string>.Ok("Producto editado con éxito");
        }

        public async Task<OperationResult<string>> EliminarProducto(int Id)
        {
            var producto = await _productoRepository.ObtenerProductoCompletoAsync(Id);
            if (producto == null)
                return OperationResult<string>.Fail("No hay productos para eliminar");
            
            // 1. Elimina en BD primero
            var resultado = await _productoRepository.EliminarProductoAsync(producto);
           
            // 2. Solo borra la imagen si la BD confirmó
            if (resultado.Success && !string.IsNullOrEmpty(producto.Imagen) && !string.IsNullOrEmpty(producto.CodigoBarrasImagen))
            {
                string fileName = Path.GetFileName(producto.Imagen);
                string codigoBarras = Path.GetFileName(producto.CodigoBarrasImagen);
                await _gestorArchivos.BorrarArchivo(fileName, "imagenes");
                await _gestorArchivos.BorrarArchivo(codigoBarras, "barcodes");

            }

            _logger.LogInformation("Producto con ID {Id} eliminado correctamente.", Id);
            return OperationResult<string>.Ok("Producto eliminado con exito");
        }

    }
}
