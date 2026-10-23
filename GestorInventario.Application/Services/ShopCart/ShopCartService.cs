using GestorInventario.Application.Interfaces.Application.Services.Orders;
using GestorInventario.Application.Interfaces.Application.Services.ShopCart;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Application.Interfaces.Web;
using GestorInventario.Application.Services.Common;
using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Application.Services.ShopCart
{
    public class ShopCartService : IShopCartService
    {
        private readonly ICarritoRepository _carritoRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly ILogger<ShopCartService> _logger;
        private readonly IOrderService _pedidoService;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IProductoRepository _productoRepository;

        public ShopCartService(ICarritoRepository carritoRepository, IOrderService pedido, IProductoRepository producto,
            ICurrentUserAccessor currentUserAccessor, ILogger<ShopCartService> logger,
            IPedidoRepository pedidoRepository)
        {
            _carritoRepository = carritoRepository;
            _currentUserAccessor = currentUserAccessor;
            _logger = logger;
            _pedidoService = pedido;
            _pedidoRepository = pedidoRepository;
            _productoRepository = producto;
        }

        public async Task EliminarCarritoActivoAsync()
        {
            var usuarioId = _currentUserAccessor.GetCurrentUserId();

            if (usuarioId <= 0)
            {
                _logger.LogWarning("Intento de eliminar carrito sin usuario autenticado");
                return;
            }

            try
            {
                // 1. Obtener el carrito activo del usuario (debería ser solo uno)
                var carritoActivo = await _carritoRepository.ObtenerCarritoUsuario(usuarioId);

                if (carritoActivo == null)
                {
                    _logger.LogDebug("No se encontró carrito activo para el usuario {UsuarioId}", usuarioId);
                    return;
                }

                // 2. Verificar si tiene items
                var items = await _carritoRepository.ObtenerItemsDelCarritoUsuario(carritoActivo.Id);

                if (items.Any())
                {
                    _logger.LogInformation("Carrito activo del usuario {UsuarioId} tiene items → no se elimina",
                        usuarioId);
                    return;
                }

                // 3. Eliminar 
                await _pedidoService.EliminarPedido(carritoActivo.Id);

                _logger.LogInformation("Carrito activo vacío eliminado para el usuario {UsuarioId}", usuarioId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar eliminar el carrito activo del usuario {UsuarioId}", usuarioId);
            }
        }

        // Método para crear un carrito si no existe
        public async Task<OperationResult<Pedido>> CrearCarritoUsuario(int userId)
        {
            try
            {
                var carrito = await _carritoRepository.ObtenerCarritoUsuario(userId);

                if (carrito == null)
                {
                    carrito = new Pedido
                    {
                        IdUsuario = userId,
                        NumeroPedido = GenerarNumPedido.GenerarNumeroPedido(),
                        FechaPedido = DateTime.UtcNow,
                        EstadoPedido = EstadoPedido.Carrito.ToString(),
                        EsCarrito = true,
                        Currency = "EUR"
                    };
                    await _pedidoRepository.AgregarPedidoAsync(carrito);
                }

                return OperationResult<Pedido>.Ok(carrito);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrio un error inesperado");
                return OperationResult<Pedido>.Fail("Error al crear el carrito");
            }
        }

        public async Task<OperationResult<string>> Incremento(int id)
        {
            var detalle = await _carritoRepository.ItemsDelCarrito(id);

            if (detalle == null)
            {
                _logger.LogError("Item no encontrado");
                return OperationResult<string>.Fail();
            }

            detalle.Cantidad++;
            await _pedidoRepository.ActualizarDetallePedidoAsync(detalle);

            var producto = await _productoRepository.ObtenerProductoPorIdAsync(detalle.ProductoId);
            if (producto == null || producto.Cantidad <= 0)
            {
                return OperationResult<string>.Fail("Inventario insuficiente para el producto elegido");
            }

            producto.Cantidad--;
            await _productoRepository.ActualizarProductoAsync(producto);

            return OperationResult<string>.Ok("Incremento realizado con exito");
        }

        public async Task<OperationResult<string>> AgregarProductoAlCarrito(int idProducto, int cantidad, int usuarioId)
        {
            // Validar cantidad
            if (cantidad <= 0)
            {
                return OperationResult<string>.Fail("La cantidad debe ser mayor a cero.");
            }

            // Validar existencia del producto y stock
            var producto = await _productoRepository.ObtenerProductoPorIdAsync(idProducto);
            if (producto == null)
            {
                return OperationResult<string>.Fail();
            }

            if (producto.Cantidad < cantidad)
            {
                return OperationResult<string>.Fail("No hay suficientes productos en stock.");
            }

            // Obtener o crear el carrito
            var carritoResult = await CrearCarritoUsuario(usuarioId);
            if (carritoResult.IsSuccess)
            {
                var carrito = carritoResult.Unwrap();
                var detalleExistente = await _productoRepository.ObtenerDetallesCarrito(carrito.Id, idProducto);
                if (detalleExistente != null)
                {
                    // Sumar la cantidad al ítem existente
                    detalleExistente.Cantidad += cantidad;
                    await _pedidoRepository.ActualizarDetallePedidoAsync(detalleExistente);
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
                    await _pedidoRepository.AgregarDetallePedidoAsync(detalle);
                }
            }

            // Actualizar el inventario del producto
            producto.Cantidad -= cantidad;
            await _productoRepository.ActualizarProductoAsync(producto);
            return OperationResult<string>.Ok("Producto agregado con exito");
        }

        public async Task<OperationResult<string>> Decremento(int id)
        {
            var detalle = await _carritoRepository.ItemsDelCarrito(id);

            if (detalle == null)
            {
                _logger.LogError("Item no encontrado");
                return OperationResult<string>.Fail();
            }

            detalle.Cantidad--;
            if (detalle.Cantidad <= 0)
            {
                await _pedidoRepository.EliminarDetallePedidoAsync(detalle);
            }
            else
            {
                await _pedidoRepository.ActualizarDetallePedidoAsync(detalle);
            }

            var producto = await _productoRepository.ObtenerProductoPorIdAsync(detalle.ProductoId);
            if (producto == null)
            {
                return OperationResult<string>.Fail();
            }

            producto.Cantidad++;
            await _productoRepository.ActualizarProductoAsync(producto);


            return OperationResult<string>.Ok("Incremento realizado con exito");
        }

        public async Task<OperationResult<string>> EliminarProductoCarrito(int id)
        {
            var detalle = await _pedidoRepository.ObtenerDetallePorIdAsync(id);
            if (detalle == null)
            {
                return OperationResult<string>.Fail();
            }

            var producto = await _productoRepository.ObtenerProductoPorIdAsync(detalle.ProductoId);
            if (producto != null)
            {
                producto.Cantidad += detalle.Cantidad;
                await _productoRepository.ActualizarProductoAsync(producto);
            }

            await _pedidoRepository.EliminarDetallePedidoAsync(detalle);

            return OperationResult<string>.Ok("Eliminacion exitosa del producto");
        }
    }
}