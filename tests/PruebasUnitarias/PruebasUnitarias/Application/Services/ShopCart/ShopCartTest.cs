using Castle.Core.Logging;
using GestorInventario.Application.Services.Carrito;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Orders;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Web;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace PruebasUnitarias.Application.Services.ShopCart
{
    public class ShopCartTest
    {
        private readonly Mock<ICarritoRepository> _mockCarrito;
        private readonly Mock<ICurrentUserAccessor> _mockUserAccessor;
        private readonly Mock<ILogger<ShopCartService>> _mockLogger;
        private readonly Mock<IOrderService> _mockOrder;
        private readonly Mock<IPedidoRepository> _mockPedido;
        private readonly Mock<IProductoRepository> _mockProducto;
        private readonly ShopCartService _sut;

        public ShopCartTest() {
        
        _mockCarrito = new Mock<ICarritoRepository>();
        _mockUserAccessor = new Mock<ICurrentUserAccessor>();
        _mockLogger = new Mock<ILogger<ShopCartService>>();
        _mockPedido = new Mock<IPedidoRepository>();
        _mockProducto = new Mock<IProductoRepository>();
        _mockOrder= new Mock<IOrderService>();
            _sut = new ShopCartService(_mockCarrito.Object, _mockOrder.Object, _mockProducto.Object, _mockUserAccessor.Object, _mockLogger.Object, _mockPedido.Object);
        }
        // ============================================================
        // Test 1: Elimina el carrito exitosamente.
     
        // ============================================================
        [Fact]
        public async Task Eliminar_carrito_test()
        {
            //Arrage
            int userId = 1;
            var carritoExistente = new Pedido
            {
                Id = 10,
                IdUsuario = userId,
                EstadoPedido = "Carrito",
                EsCarrito = true
            };
            _mockUserAccessor.Setup(r=>r.GetCurrentUserId()).Returns(userId);
            _mockCarrito.Setup(r => r.ObtenerCarritoUsuario(userId)).Returns(Task.FromResult(carritoExistente));
            _mockCarrito.Setup(r => r.ObtenerItemsDelCarritoUsuario(10)) 
      .Returns(Task.FromResult(new List<DetallePedido>()));
            _mockOrder.Setup(r => r.EliminarPedido(carritoExistente.Id)).Returns(Task.FromResult(OperationResult<string>.Ok()));
            //Act
            await _sut.EliminarCarritoActivoAsync();
            //Assert
            _mockOrder.Verify(p => p.EliminarPedido(carritoExistente.Id), Times.Once);

            // Verificamos que se logueó la eliminación
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Carrito activo vacío eliminado")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        // ============================================================
        // Test 2: Usuario NO autenticado (ID <= 0).
        // El método entra, ve que usuarioId = 0, loguea warning y sale.
        // NUNCA debe tocar el repositorio de carrito.
        // ============================================================
        [Fact]
        public async Task EliminarCarritoActivoAsync_UsuarioNoAutenticado_LogueaWarningYNoTocaRepo()
        {
            // Arrange
            // Mentira: el usuario actual tiene ID 0 (no logueado)
            _mockUserAccessor.Setup(u => u.GetCurrentUserId()).Returns(0);

            // Act
            await _sut.EliminarCarritoActivoAsync();

            // Assert
            // Verificamos que NUNCA se llamó a ObtenerCarritoUsuario
            _mockCarrito.Verify(c => c.ObtenerCarritoUsuario(It.IsAny<int>()), Times.Never);

            // Verificamos que se logueó el warning exacto
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("sin usuario autenticado")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        // ============================================================
        // Test 3: Usuario autenticado, pero NO tiene carrito activo.
        // ObtenerCarritoUsuario devuelve null. Loguea debug y sale.
        // ============================================================
        [Fact]
        public async Task EliminarCarritoActivoAsync_SinCarrito_LogueaDebugYNoElimina()
        {
            // Arrange
            int userId = 1;

            // Mentira 1: el usuario está autenticado
            _mockUserAccessor.Setup(u => u.GetCurrentUserId()).Returns(userId);

            // Mentira 2: el repositorio dice "no tengo carrito para este usuario"
            _mockCarrito.Setup(c => c.ObtenerCarritoUsuario(userId))
                .Returns(Task.FromResult<Pedido>(null!));

            // Act
            await _sut.EliminarCarritoActivoAsync();

            // Assert
            // NUNCA debe preguntar por items (si no hay carrito, no hay items)
            _mockCarrito.Verify(c => c.ObtenerItemsDelCarritoUsuario(It.IsAny<int>()), Times.Never);

            // NUNCA debe intentar eliminar
            _mockOrder.Verify(o => o.EliminarPedido(It.IsAny<int>()), Times.Never);

            // Verificamos que se logueó que no se encontró carrito
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No se encontró carrito activo")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        // ============================================================
        // Test 4: Usuario autenticado, tiene carrito, pero el carrito TIENE items.
        // No se elimina. Loguea info y sale.
        // ============================================================
        [Fact]
        public async Task EliminarCarritoActivoAsync_CarritoConItems_NoEliminaYLogueaInfo()
        {
            // Arrange
            int userId = 1;
            int carritoId = 10;

            // Mentira 1: usuario autenticado
            _mockUserAccessor.Setup(u => u.GetCurrentUserId()).Returns(userId);

            // Mentira 2: existe un carrito
            var carrito = new Pedido
            {
                Id = carritoId,
                IdUsuario = userId,
                EsCarrito = true
            };
            _mockCarrito.Setup(c => c.ObtenerCarritoUsuario(userId))
                .Returns(Task.FromResult(carrito));

            // Mentira 3: el carrito TIENE items (lista NO vacía)
            _mockCarrito.Setup(c => c.ObtenerItemsDelCarritoUsuario(carritoId))
                .Returns(Task.FromResult(new List<DetallePedido>
                {
            new DetallePedido { Id = 1, Cantidad = 2 }
                }));

            // Act
            await _sut.EliminarCarritoActivoAsync();

            // Assert
            // NUNCA debe llamar a eliminar porque tiene items
            _mockOrder.Verify(o => o.EliminarPedido(It.IsAny<int>()), Times.Never);

            // Verificamos que se logueó que tiene items
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("tiene items")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        // ============================================================
        // Test 5: Excepción inesperada en el repositorio.
        // El try-catch la atrapa, loguea error y sale.
        // ============================================================
        [Fact]
        public async Task EliminarCarritoActivoAsync_ExcepcionEnRepo_LogueaErrorYNoRompe()
        {
            // Arrange
            int userId = 1;

            // Mentira 1: usuario autenticado
            _mockUserAccessor.Setup(u => u.GetCurrentUserId()).Returns(userId);

            // Mentira 2: el repositorio explota (simulamos un error de BD)
            _mockCarrito.Setup(c => c.ObtenerCarritoUsuario(userId))
                .ThrowsAsync(new Exception("Timeout en base de datos"));

            // Act
            // Esto NO debe lanzar excepción porque el método tiene try-catch
            await _sut.EliminarCarritoActivoAsync();

            // Assert
            // Verificamos que se logueó el error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error al intentar eliminar")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        // -----------------------------------------------------------
        // Test 1: El usuario YA tiene carrito. 
        // Debe devolver el carrito existente SIN crear uno nuevo.
        // -----------------------------------------------------------
        [Fact]
        public async Task CrearCarritoUsuario_CarritoExistente_DevuelveOkSinCrearNuevo()
        {
            // Arrange
            int userId = 1;
            var carritoExistente = new Pedido
            {
                Id = 10,
                IdUsuario = userId,
                EstadoPedido = "Carrito",
                EsCarrito = true
            };

          
            // CORRECTO: devolvemos el Pedido directamente (sin OperationResult)
            _mockCarrito.Setup(r => r.ObtenerCarritoUsuario(userId))
                .Returns(Task.FromResult(carritoExistente));

            // Act
            var resultado = await _sut.CrearCarritoUsuario(userId);

            // Assert
            Assert.True(resultado.Success);
            Assert.Equal(carritoExistente, resultado.Data);

            // Verificamos que NO se intentó crear un carrito nuevo
            _mockPedido.Verify(p => p.AgregarPedidoAsync(It.IsAny<Pedido>()), Times.Never);
        }

        // -----------------------------------------------------------
        // Test 2: El usuario NO tiene carrito.
        // Debe crear uno nuevo, guardarlo, y devolverlo.
        // -----------------------------------------------------------
        [Fact]
        public async Task CrearCarritoUsuario_SinCarrito_CreaNuevoYDevuelveOk()
        {
            // Arrange
            int userId = 2;

            // El repositorio devuelve NULL (no tiene carrito)
            _mockCarrito.Setup(r => r.ObtenerCarritoUsuario(userId))
                .Returns(Task.FromResult<Pedido>(null!));

            // El repositorio de pedidos guarda correctamente
            _mockPedido.Setup(p => p.AgregarPedidoAsync(It.IsAny<Pedido>()))
                .Returns(Task.FromResult(OperationResult<Pedido>.Ok("Guardado")));

            // Act
            var resultado = await _sut.CrearCarritoUsuario(userId);

            // Assert
            Assert.True(resultado.Success);
            Assert.NotNull(resultado.Data);
            Assert.Equal(userId, resultado.Data.IdUsuario);
            Assert.True(resultado.Data.EsCarrito);
            Assert.Equal("Carrito", resultado.Data.EstadoPedido);
            Assert.Equal("EUR", resultado.Data.Currency);

            // Verificamos que SÍ se llamó a guardar el nuevo pedido
            _mockPedido.Verify(p => p.AgregarPedidoAsync(It.Is<Pedido>(
                ped => ped.IdUsuario == userId && ped.EsCarrito)), Times.Once);
        }

        // -----------------------------------------------------------
        // Test 3: El repositorio lanza una excepción inesperada.
        // Debe loguear el error y devolver Fail.
        // -----------------------------------------------------------
        [Fact]
        public async Task CrearCarritoUsuario_ExcepcionEnRepo_LogueaErrorYDevuelveFail()
        {
            // Arrange
            int userId = 3;

            // Forzamos una excepción en el repositorio
            _mockCarrito.Setup(r => r.ObtenerCarritoUsuario(userId))
                .ThrowsAsync(new Exception("BD caída"));

            // Act
            var resultado = await _sut.CrearCarritoUsuario(userId);

            // Assert
            Assert.False(resultado.Success);
            Assert.Equal("Error al crear el carrito", resultado.Message);

            // Verificamos que se logueó el error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Ocurrio un error inesperado")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        // ============================================================
        // Incremento
        // ============================================================

        [Fact]
        public async Task Incremento_ItemExiste_ProductoConStock_IncrementaCantidadYReduceStock()
        {
            // Arrange
            int detalleId = 1;
            var detalle = new DetallePedido
            {
                Id = detalleId,
                ProductoId = 5,
                Cantidad = 2
            };
            var producto = new Producto
            {
                Id = 5,
                Cantidad = 10
            };

            _mockCarrito.Setup(c => c.ItemsDelCarrito(detalleId))
                .Returns(Task.FromResult(detalle));
            _mockPedido.Setup(p => p.ActualizarDetallePedidoAsync(detalle))
                .Returns(Task.FromResult(OperationResult<DetallePedido>.Ok()));
            _mockProducto.Setup(p => p.ObtenerProductoPorIdAsync(5))
                .Returns(Task.FromResult(producto));
            _mockProducto.Setup(p => p.ActualizarProductoAsync(producto))
                .Returns(Task.FromResult(OperationResult<Producto>.Ok()));

            // Act
            var resultado = await _sut.Incremento(detalleId);

            // Assert
            Assert.True(resultado.Success);
            Assert.Equal("Incremento realizado con exito", resultado.Message);
            Assert.Equal(3, detalle.Cantidad);      // 2 + 1
            Assert.Equal(9, producto.Cantidad);     // 10 - 1
        }

        [Fact]
        public async Task Incremento_ProductoSinStock_DevuelveFail()
        {
            // Arrange
            int detalleId = 1;
            var detalle = new DetallePedido
            {
                Id = detalleId,
                ProductoId = 5,
                Cantidad = 2
            };
            var producto = new Producto
            {
                Id = 5,
                Cantidad = 0  // Sin stock
            };

            _mockCarrito.Setup(c => c.ItemsDelCarrito(detalleId))
                .Returns(Task.FromResult(detalle));
            _mockPedido.Setup(p => p.ActualizarDetallePedidoAsync(detalle))
                .Returns(Task.FromResult(OperationResult<DetallePedido>.Fail("")));
            _mockProducto.Setup(p => p.ObtenerProductoPorIdAsync(5))
                .Returns(Task.FromResult(producto));

            // Act
            var resultado = await _sut.Incremento(detalleId);

            // Assert
            Assert.False(resultado.Success);
            Assert.Contains("Inventario insuficiente", resultado.Message);
        }

        // ============================================================
        // Decremento
        // ============================================================

        [Fact]
        public async Task Decremento_ItemExiste_CantidadMayorA1_DecrementaYActualiza()
        {
            // Arrange
            int detalleId = 1;
            var detalle = new DetallePedido
            {
                Id = detalleId,
                ProductoId = 5,
                Cantidad = 3
            };
            var producto = new Producto { Id = 5, Cantidad = 10 };

            _mockCarrito.Setup(c => c.ItemsDelCarrito(detalleId))
                .Returns(Task.FromResult(detalle));
            _mockPedido.Setup(p => p.ActualizarDetallePedidoAsync(detalle))
                .Returns(Task.FromResult(OperationResult<DetallePedido>.Ok()));
            _mockProducto.Setup(p => p.ObtenerProductoPorIdAsync(5))
                .Returns(Task.FromResult(producto));
            _mockProducto.Setup(p => p.ActualizarProductoAsync(producto))
                .Returns(Task.FromResult(OperationResult<Producto>.Ok()));

            // Act
            var resultado = await _sut.Decremento(detalleId);

            // Assert
            Assert.True(resultado.Success);
            Assert.Equal(2, detalle.Cantidad);      // 3 - 1
            Assert.Equal(11, producto.Cantidad);    // 10 + 1
            // Verificamos que NO se llamó a eliminar (porque cantidad > 0)
            _mockPedido.Verify(p => p.EliminarDetallePedidoAsync(detalle), Times.Never);
        }

        [Fact]
        public async Task Decremento_ItemExiste_CantidadLlegaA0_EliminaDetalle()
        {
            // Arrange
            int detalleId = 1;
            var detalle = new DetallePedido
            {
                Id = detalleId,
                ProductoId = 5,
                Cantidad = 1  // Al decrementar queda en 0
            };
            var producto = new Producto { Id = 5, Cantidad = 10 };

            _mockCarrito.Setup(c => c.ItemsDelCarrito(detalleId))
                .Returns(Task.FromResult(detalle));
            _mockPedido.Setup(p => p.EliminarDetallePedidoAsync(detalle))
                .Returns(Task.FromResult(OperationResult<DetallePedido>.Fail("")));
            _mockProducto.Setup(p => p.ObtenerProductoPorIdAsync(5))
                .Returns(Task.FromResult(producto));
            _mockProducto.Setup(p => p.ActualizarProductoAsync(producto))
                .Returns(Task.FromResult(OperationResult<Producto>.Fail("")));

            // Act
            var resultado = await _sut.Decremento(detalleId);

            // Assert
            Assert.True(resultado.Success);
            // Verificamos que SÍ se llamó a eliminar (porque cantidad quedó en 0)
            _mockPedido.Verify(p => p.EliminarDetallePedidoAsync(detalle), Times.Once);
            Assert.Equal(11, producto.Cantidad); // El stock se devuelve igual
        }

        // ============================================================
        // AgregarProductoAlCarrito
        // ============================================================

        [Fact]
        public async Task AgregarProductoAlCarrito_CantidadInvalida_DevuelveFail()
        {
            // Act
            var resultado = await _sut.AgregarProductoAlCarrito(1, 0, 1);

            // Assert
            Assert.False(resultado.Success);
            Assert.Equal("La cantidad debe ser mayor a cero.", resultado.Message);
        }

        [Fact]
        public async Task AgregarProductoAlCarrito_ProductoSinStock_DevuelveFail()
        {
            // Arrange
            var producto = new Producto { Id = 1, Cantidad = 2 };
            _mockProducto.Setup(p => p.ObtenerProductoPorIdAsync(1))
                .Returns(Task.FromResult(producto));

            // Act: pide 5 pero solo hay 2
            var resultado = await _sut.AgregarProductoAlCarrito(1, 5, 1);

            // Assert
            Assert.False(resultado.Success);
            Assert.Contains("No hay suficientes productos en stock", resultado.Message);
        }

        [Fact]
        public async Task AgregarProductoAlCarrito_ProductoNuevoEnCarrito_AgregaDetalleYReduceStock()
        {
            // Arrange
            int userId = 1;
            int productoId = 5;
            int cantidad = 2;
            var carrito = new Pedido { Id = 10, IdUsuario = userId, EsCarrito = true };
            var producto = new Producto { Id = productoId, Cantidad = 10 };

            // CrearCarritoUsuario necesita que el mock devuelva un carrito existente
            _mockCarrito.Setup(c => c.ObtenerCarritoUsuario(userId))
                .Returns(Task.FromResult(carrito));

            _mockProducto.Setup(p => p.ObtenerProductoPorIdAsync(productoId))
                .Returns(Task.FromResult(producto));

            // El producto NO está aún en el carrito (devuelve null)
            _mockProducto.Setup(p => p.ObtenerDetallesCarrito(carrito.Id, productoId))
                .Returns(Task.FromResult<DetallePedido>(null!));

            _mockPedido.Setup(p => p.AgregarDetallePedidoAsync(It.IsAny<DetallePedido>()))
                .Returns(Task.FromResult(OperationResult<DetallePedido>.Ok()));
            _mockProducto.Setup(p => p.ActualizarProductoAsync(producto))
                .Returns(Task.FromResult(OperationResult<Producto>.Ok()));

            // Act
            var resultado = await _sut.AgregarProductoAlCarrito(productoId, cantidad, userId);

            // Assert
            Assert.True(resultado.Success);
            Assert.Equal("Producto agregado con exito", resultado.Message);
            Assert.Equal(8, producto.Cantidad); // 10 - 2

            // Verificamos que se agregó el detalle con los datos correctos
            _mockPedido.Verify(p => p.AgregarDetallePedidoAsync(
                It.Is<DetallePedido>(d => d.PedidoId == 10 && d.ProductoId == 5 && d.Cantidad == 2)),
                Times.Once);
        }

        [Fact]
        public async Task AgregarProductoAlCarrito_ProductoExistenteEnCarrito_SumaCantidadYReduceStock()
        {
            // Arrange
            int userId = 1;
            int productoId = 5;
            int cantidad = 3;
            var carrito = new Pedido { Id = 10, IdUsuario = userId, EsCarrito = true };
            var producto = new Producto { Id = productoId, Cantidad = 10 };
            var detalleExistente = new DetallePedido
            {
                Id = 1,
                PedidoId = 10,
                ProductoId = productoId,
                Cantidad = 2
            };

            _mockCarrito.Setup(c => c.ObtenerCarritoUsuario(userId))
                .Returns(Task.FromResult(carrito));
            _mockProducto.Setup(p => p.ObtenerProductoPorIdAsync(productoId))
                .Returns(Task.FromResult(producto));
            // El producto YA está en el carrito
            _mockProducto.Setup(p => p.ObtenerDetallesCarrito(carrito.Id, productoId))
                .Returns(Task.FromResult(detalleExistente));
            _mockPedido.Setup(p => p.ActualizarDetallePedidoAsync(detalleExistente))
               .Returns(Task.FromResult(OperationResult<DetallePedido>.Ok()));
            _mockProducto.Setup(p => p.ActualizarProductoAsync(producto))
                .Returns(Task.FromResult(OperationResult<Producto>.Ok()));

            // Act
            var resultado = await _sut.AgregarProductoAlCarrito(productoId, cantidad, userId);

            // Assert
            Assert.True(resultado.Success);
            Assert.Equal(5, detalleExistente.Cantidad); // 2 + 3
            Assert.Equal(7, producto.Cantidad);         // 10 - 3
            // Verificamos que NO se agregó un detalle nuevo, sino que se actualizó
            _mockPedido.Verify(p => p.AgregarDetallePedidoAsync(It.IsAny<DetallePedido>()), Times.Never);
            _mockPedido.Verify(p => p.ActualizarDetallePedidoAsync(detalleExistente), Times.Once);
        }

        // ============================================================
        // EliminarProductoCarrito
        // ============================================================

        [Fact]
        public async Task EliminarProductoCarrito_DetalleNoExiste_DevuelveFail()
        {
            // Arrange
            _mockPedido.Setup(p => p.ObtenerDetallePorIdAsync(99))
                .Returns(Task.FromResult<DetallePedido>(null!));

            // Act
            var resultado = await _sut.EliminarProductoCarrito(99);

            // Assert
            Assert.False(resultado.Success);
            Assert.Contains("No se puede eliminar porque no hay productos", resultado.Message);
        }

        [Fact]
        public async Task EliminarProductoCarrito_DetalleExiste_DevuelveStockYElimina()
        {
            // Arrange
            int detalleId = 1;
            var detalle = new DetallePedido
            {
                Id = detalleId,
                ProductoId = 5,
                Cantidad = 3
            };
            var producto = new Producto { Id = 5, Cantidad = 10 };

            _mockPedido.Setup(p => p.ObtenerDetallePorIdAsync(detalleId))
                .Returns(Task.FromResult(detalle));
            _mockProducto.Setup(p => p.ObtenerProductoPorIdAsync(5))
                .Returns(Task.FromResult(producto));
            _mockProducto.Setup(p => p.ActualizarProductoAsync(producto))
                .Returns(Task.FromResult(OperationResult<Producto>.Ok()));
            _mockPedido.Setup(p => p.EliminarDetallePedidoAsync(detalle))
                .Returns(Task.FromResult(OperationResult<DetallePedido>.Ok()));

            // Act
            var resultado = await _sut.EliminarProductoCarrito(detalleId);

            // Assert
            Assert.True(resultado.Success);
            Assert.Equal("Eliminacion exitosa del producto", resultado.Message);
            Assert.Equal(13, producto.Cantidad); // 10 + 3 (devuelve el stock)
            _mockPedido.Verify(p => p.EliminarDetallePedidoAsync(detalle), Times.Once);
        }
    }
}


