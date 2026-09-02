using GestorInventario.Application.Services.Products;
using GestorInventario.Domain.enums.Productos;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Common;
using GestorInventario.Interfaces.Application.Services.Products;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Renderer.Images;
using GestorInventario.Interfaces.Web;
using GestorInventario.Shared.DTOS.Barcode;
using GestorInventario.Shared.DTOS.Products;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace GestorInventario.PruebasUnitarias.Application.Services.Products
{
    /// <summary>
    /// Tests para ProductService.
    /// Este servicio maneja archivos, imágenes y códigos de barras,
    /// pero nosotros NO testeamos eso. Testeamos las decisiones de negocio:
    /// - Validar nombre duplicado
    /// - Solo borrar archivos si la BD confirmó
    /// - Manejar producto no encontrado
    /// Los servicios de archivos/imágenes/barcode se mockean (son infraestructura).
    /// </summary>
    public class ProductServiceTests
    {
        private readonly Mock<IGestorArchivos> _mockArchivos;
        private readonly Mock<IBarCodeService> _mockBarcode;
        private readonly Mock<ILogger<ProductService>> _mockLogger;
        private readonly Mock<IProductoRepository> _mockRepo;
        private readonly Mock<IImageOptimizerService> _mockImageOptimizer;
        private readonly ProductService _sut;

        public ProductServiceTests()
        {
            _mockArchivos = new Mock<IGestorArchivos>();
            _mockBarcode = new Mock<IBarCodeService>();
            _mockLogger = new Mock<ILogger<ProductService>>();
            _mockRepo = new Mock<IProductoRepository>();
            _mockImageOptimizer = new Mock<IImageOptimizerService>();

            _sut = new ProductService(
                _mockArchivos.Object,
                _mockBarcode.Object,
                _mockLogger.Object,
                _mockRepo.Object,
                _mockImageOptimizer.Object);
        }

        // ============================================================
        // CrearProducto
        // ============================================================

        [Fact]
        public async Task CrearProducto_NombreDuplicado_DevuelveFail()
        {
            // Arrange
            var dto = new ProductoDto
            {
                NombreProducto = "ProductoExistente",
                Cantidad = 10,
                Precio = 100,
                Descripcion="Producto"
            };

            // El barcode se genera SIEMPRE (lo hace antes de la validación)
            _mockBarcode.Setup(b => b.GenerateUniqueBarCodeAsync(
                    It.IsAny<BarcodeType>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(new BarcodeResultDto { Code = "123", ImagePath = "/img/bar.png" }));

            // El repositorio dice que YA existe un producto con ese nombre
            _mockRepo.Setup(r => r.ExisteNombreProductoAsync("ProductoExistente"))
                .Returns(Task.FromResult(true));

            // Act
            var resultado = await _sut.CrearProducto(dto);

            // Assert
            Assert.False(resultado.Success);
            Assert.Contains("Ya hay un producto con ese nombre", resultado.Message);

            // Verificamos que NUNCA intentó guardar en BD
            _mockRepo.Verify(r => r.AgregarProductoAsync(It.IsAny<Producto>()), Times.Never);
        }

        [Fact]
        public async Task CrearProducto_CaminoFelizSinImagen_CreaProductoYDevuelveOk()
        {
            // Arrange
            var dto = new ProductoDto
            {
                NombreProducto = "NuevoProducto",
                Cantidad = 10,
                Precio = 100,
                Descripcion="nuevo"
            };

            _mockBarcode.Setup(b => b.GenerateUniqueBarCodeAsync(
                    It.IsAny<BarcodeType>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(new BarcodeResultDto { Code = "ABC123", ImagePath = "/img/bar.png" }));

            _mockRepo.Setup(r => r.ExisteNombreProductoAsync("NuevoProducto"))
                .Returns(Task.FromResult(false));

            _mockRepo.Setup(r => r.AgregarProductoAsync(It.IsAny<Producto>()))
                .Returns(Task.FromResult(OperationResult<Producto>.Ok()));

            // Act
            var resultado = await _sut.CrearProducto(dto);

            // Assert
            Assert.True(resultado.Success);
            Assert.NotNull(resultado.Data);
            Assert.Equal("NuevoProducto", resultado.Data.NombreProducto);
            Assert.Equal("ABC123", resultado.Data.CodigoBarras);
            Assert.True(string.IsNullOrEmpty(resultado.Data.Imagen)); // No tiene imagen

            // Verificamos que NO se llamó al optimizador de imágenes
            _mockImageOptimizer.Verify(i => i.OptimizeAndSaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CrearProducto_CaminoFelizConImagen_ProcesaImagenYCreaProducto()
        {
            // Arrange
            var dto = new ProductoDto
            {
                NombreProducto = "ProductoConFoto",
                Cantidad = 5,
                Precio = 50,
                ArchivoImagenBytes = new byte[] { 1, 2, 3 }, // ← SÍ tiene imagen
                ArchivoImagenNombre = "foto.jpg",
                Descripcion="producto con foto"
            };

            _mockBarcode.Setup(b => b.GenerateUniqueBarCodeAsync(
                    It.IsAny<BarcodeType>(), It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(Task.FromResult(new BarcodeResultDto { Code = "XYZ789", ImagePath = "/img/bar.png" }));

            _mockRepo.Setup(r => r.ExisteNombreProductoAsync("ProductoConFoto"))
                .Returns(Task.FromResult(false));

            // El optimizador de imágenes devuelve una ruta ficticia
            _mockImageOptimizer.Setup(i => i.OptimizeAndSaveImageAsync(
                    dto.ArchivoImagenBytes, dto.ArchivoImagenNombre, "imagenes"))
                .Returns(Task.FromResult("/imagenes/foto_optimizada.jpg"));

            _mockRepo.Setup(r => r.AgregarProductoAsync(It.IsAny<Producto>()))
                .Returns(Task.FromResult(OperationResult<Producto>.Ok()));

            // Act
            var resultado = await _sut.CrearProducto(dto);

            // Assert
            Assert.True(resultado.Success);
            Assert.Equal("/imagenes/foto_optimizada.jpg", resultado.Data.Imagen);
        }

        // ============================================================
        // EditarProducto
        // ============================================================

        [Fact]
        public async Task EditarProducto_ProductoNoExiste_DevuelveFail()
        {
            // Arrange
            var dto = new EditarProductoDto { Id = 99, NombreProducto = "Cualquiera",Descripcion="cualquiera" };

            _mockRepo.Setup(r => r.ObtenerProductoPorIdAsync(99))
                .Returns(Task.FromResult<Producto>(null!));

            // Act
            var resultado = await _sut.EditarProducto(dto, usuarioId: 1);

            // Assert
            Assert.False(resultado.Success);
            Assert.Equal("Producto no encontrado", resultado.Message);
        }

        [Fact]
        public async Task EditarProducto_SinCambioDeImagen_ActualizaSinTocarArchivos()
        {
            // Arrange
            var dto = new EditarProductoDto
            {
                Id = 1,
                NombreProducto = "ProductoEditado",
                Cantidad = 20,
                Precio = 200,
                Descripcion="Edita"
                // Sin imagen nueva
            };

            var productoExistente = new Producto
            {
                Id = 1,
                NombreProducto = "ProductoViejo",
                Imagen = "/imagenes/vieja.jpg",
                Cantidad = 10,
                Precio = 100
            };

            _mockRepo.Setup(r => r.ObtenerProductoPorIdAsync(1))
                .Returns(Task.FromResult(productoExistente));

            _mockRepo.Setup(r => r.ActualizarProductoAsync(productoExistente))
                .Returns(Task.FromResult(OperationResult<Producto>.Ok()));

            // Act
            var resultado = await _sut.EditarProducto(dto, usuarioId: 1);

            // Assert
            Assert.True(resultado.Success);
            // La imagen sigue siendo la vieja (no se cambió)
            Assert.Equal("/imagenes/vieja.jpg", productoExistente.Imagen);

            // Verificamos que NO se llamó al optimizador (no había imagen nueva)
            _mockImageOptimizer.Verify(i => i.OptimizeAndSaveImageAsync(
                It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            // Verificamos que NO se borró la imagen anterior (no se generó nueva)
            _mockArchivos.Verify(a => a.BorrarArchivo(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task EditarProducto_ConImagenNueva_BorraAnteriorSiBdConfirma()
        {
            // Arrange
            var dto = new EditarProductoDto
            {
                Id = 1,
                NombreProducto = "ProductoEditado",
                Cantidad = 20,
                Precio = 200,
                ArchivoImagenBytes = new byte[] { 4, 5, 6 },
                ArchivoImagenNombre = "nueva.jpg",
                Descripcion="editado"
            };

            var productoExistente = new Producto
            {
                Id = 1,
                NombreProducto = "ProductoViejo",
                Imagen = "/imagenes/vieja.jpg", // ← imagen anterior que debe borrarse
                Cantidad = 10,
                Precio = 100
            };

            _mockRepo.Setup(r => r.ObtenerProductoPorIdAsync(1))
                .Returns(Task.FromResult(productoExistente));

            // El optimizador guarda la nueva imagen
            _mockImageOptimizer.Setup(i => i.OptimizeAndSaveImageAsync(
                    dto.ArchivoImagenBytes, dto.ArchivoImagenNombre, "imagenes"))
                .Returns(Task.FromResult("/imagenes/nueva_optimizada.jpg"));

            // La BD confirma el update
            _mockRepo.Setup(r => r.ActualizarProductoAsync(productoExistente))
                .Returns(Task.FromResult(OperationResult<Producto>.Ok()));

            // Act
            var resultado = await _sut.EditarProducto(dto, usuarioId: 1);

            // Assert
            Assert.True(resultado.Success);
            Assert.Equal("/imagenes/nueva_optimizada.jpg", productoExistente.Imagen);

            // Verificamos que SÍ se borró la imagen anterior (porque BD confirmó y había imagen previa)
            _mockArchivos.Verify(a => a.BorrarArchivo("vieja.jpg", "imagenes"), Times.Once);
        }

        [Fact]
        public async Task EditarProducto_BdFalla_NoBorraImagenAnterior()
        {
            // Arrange
            var dto = new EditarProductoDto
            {
                Id = 1,
                NombreProducto = "ProductoEditado",
                ArchivoImagenBytes = new byte[] { 4, 5, 6 },
                ArchivoImagenNombre = "nueva.jpg",
                Descripcion="editado"
            };

            var productoExistente = new Producto
            {
                Id = 1,
                Imagen = "/imagenes/vieja.jpg"
            };

            _mockRepo.Setup(r => r.ObtenerProductoPorIdAsync(1))
                .Returns(Task.FromResult(productoExistente));

            _mockImageOptimizer.Setup(i => i.OptimizeAndSaveImageAsync(
                    It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult("/imagenes/nueva.jpg"));

            // La BD FALLA (no confirma el update)
            _mockRepo.Setup(r => r.ActualizarProductoAsync(productoExistente))
                .Returns(Task.FromResult(OperationResult<Producto>.Fail("Error de BD")));

            // Act
            var resultado = await _sut.EditarProducto(dto, usuarioId: 1);

            // Assert
            // Verificamos que NO se borró la imagen anterior (porque BD no confirmó)
            _mockArchivos.Verify(a => a.BorrarArchivo(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        // ============================================================
        // EliminarProducto
        // ============================================================

        [Fact]
        public async Task EliminarProducto_ProductoNoExiste_DevuelveFail()
        {
            // Arrange
            _mockRepo.Setup(r => r.ObtenerProductoCompletoAsync(99))
                .Returns(Task.FromResult<Producto>(null!));

            // Act
            var resultado = await _sut.EliminarProducto(99);

            // Assert
            Assert.False(resultado.Success);
            Assert.Equal("No hay productos para eliminar", resultado.Message);
        }

        [Fact]
        public async Task EliminarProducto_ConArchivos_BorraImagenYBarcodeSiBdConfirma()
        {
            // Arrange
            var producto = new Producto
            {
                Id = 1,
                NombreProducto = "ProductoAEliminar",
                Imagen = "/imagenes/producto1.jpg",
                CodigoBarrasImagen = "/barcodes/code1.png"
            };

            _mockRepo.Setup(r => r.ObtenerProductoCompletoAsync(1))
                .Returns(Task.FromResult(producto));

            // La BD confirma la eliminación
            _mockRepo.Setup(r => r.EliminarProductoAsync(producto))
                .Returns(Task.FromResult(OperationResult<Producto>.Ok("Eliminado")));

            _mockArchivos.Setup(a => a.BorrarArchivo(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var resultado = await _sut.EliminarProducto(1);

            // Assert
            Assert.True(resultado.Success);

            // Verificamos que se borraron AMBOS archivos (imagen y barcode)
            _mockArchivos.Verify(a => a.BorrarArchivo("producto1.jpg", "imagenes"), Times.Once);
            _mockArchivos.Verify(a => a.BorrarArchivo("code1.png", "barcodes"), Times.Once);
        }

        [Fact]
        public async Task EliminarProducto_SinArchivos_EliminaSinBorrarNada()
        {
            // Arrange
            var producto = new Producto
            {
                Id = 2,
                NombreProducto = "ProductoSinFoto",
                Imagen = null,
                CodigoBarrasImagen = null
            };

            _mockRepo.Setup(r => r.ObtenerProductoCompletoAsync(2))
                .Returns(Task.FromResult(producto));

            _mockRepo.Setup(r => r.EliminarProductoAsync(producto))
                .Returns(Task.FromResult(OperationResult<Producto>.Ok("Eliminado")));

            // Act
            var resultado = await _sut.EliminarProducto(2);

            // Assert
            Assert.True(resultado.Success);

            // Verificamos que NO se intentó borrar nada (no había archivos)
            _mockArchivos.Verify(a => a.BorrarArchivo(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}