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
        private readonly IShopCartRepository _shopCartRepository;
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly ILogger<ShopCartService> _logger;
        private readonly IOrderService _pedidoService;
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;

        public ShopCartService(IShopCartRepository shopCartRepository, IOrderService pedido, IProductRepository product,
            ICurrentUserAccessor currentUserAccessor, ILogger<ShopCartService> logger,
            IOrderRepository orderRepository)
        {
            _shopCartRepository = shopCartRepository;
            _currentUserAccessor = currentUserAccessor;
            _logger = logger;
            _pedidoService = pedido;
            _orderRepository = orderRepository;
            _productRepository = product;
        }

        public async Task EliminarCarritoActivoAsync()
        {
            var usuarioId = _currentUserAccessor.GetCurrentUserId();

            if (usuarioId <= 0)
            {
                _logger.LogWarning("Intento de eliminar shopCart sin usuario autenticado");
                return;
            }

            try
            {
                // 1. Obtener el shopCart activo del usuario (debería ser solo uno)
                var carritoActivo = await _shopCartRepository.ObtenerCarritoUsuario(usuarioId);

                if (carritoActivo == null)
                {
                    _logger.LogDebug("No se encontró shopCart activo para el usuario {UsuarioId}", usuarioId);
                    return;
                }

                // 2. Verificar si tiene items
                var items = await _shopCartRepository.ObtenerItemsDelCarritoUsuario(carritoActivo.Id);

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
                _logger.LogError(ex, "Error al intentar eliminar el shopCart activo del usuario {UsuarioId}", usuarioId);
            }
        }

        // Método para crear un shopCart si no existe
        public async Task<OperationResult<Pedido>> CrearCarritoUsuario(int userId)
        {
            try
            {
                var carrito = await _shopCartRepository.ObtenerCarritoUsuario(userId);

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
                    await _orderRepository.AgregarPedidoAsync(carrito);
                }

                return OperationResult<Pedido>.Ok(carrito);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrio un error inesperado");
                return OperationResult<Pedido>.Fail("Error al crear el shopCart");
            }
        }

        public async Task<OperationResult<string>> Incremento(int id)
        {
            var detalle = await _shopCartRepository.ItemsDelCarrito(id);

            if (detalle == null)
            {
                _logger.LogError("Item no encontrado");
                return OperationResult<string>.Fail();
            }

            var producto = await _productRepository.ObtenerProductoPorIdAsync(detalle.ProductoId);
            if (producto == null || producto.Cantidad <= 0)
            {
                return OperationResult<string>.Fail("Inventario insuficiente para el product elegido");
            }

            // 1. Primero actualizamos el stock
            producto.Cantidad--;
            await _productRepository.ActualizarProductoAsync(producto);

            // 2. Luego incrementamos la cantidad en el shopCart
            detalle.Cantidad++;
            await _orderRepository.ActualizarDetallePedidoAsync(detalle);

            return OperationResult<string>.Ok("Incremento realizado con exito");
        }

        public async Task<OperationResult<string>> AgregarProductoAlCarrito(int idProducto, int cantidad, int usuarioId)
        {
            if (cantidad <= 0)
            {
                return OperationResult<string>.Fail("La cantidad debe ser mayor a cero.");
            }

            var producto = await _productRepository.ObtenerProductoPorIdAsync(idProducto);
            if (producto == null)
            {
                return OperationResult<string>.Fail();
            }

            if (producto.Cantidad < cantidad)
            {
                return OperationResult<string>.Fail("No hay suficientes productos en stock.");
            }

            var carritoResult = await CrearCarritoUsuario(usuarioId);
            if (!carritoResult.IsSuccess)
            {
                return OperationResult<string>.Fail("Error al gestionar el shopCart");
            }

            var carrito = carritoResult.Unwrap();
            var detalleExistente = await _productRepository.ObtenerDetallesCarrito(carrito.Id, idProducto);

            if (detalleExistente != null)
            {
                detalleExistente.Cantidad += cantidad;
                await _orderRepository.ActualizarDetallePedidoAsync(detalleExistente);
            }
            else
            {
                var detalle = new DetallePedido
                {
                    PedidoId = carrito.Id,
                    ProductoId = idProducto,
                    Cantidad = cantidad
                };
                await _orderRepository.AgregarDetallePedidoAsync(detalle);
            }

            // Actualizar el inventario (Reserva de stock)
            producto.Cantidad -= cantidad;
            await _productRepository.ActualizarProductoAsync(producto);

            return OperationResult<string>.Ok("Producto agregado con exito");
        }

        public async Task<OperationResult<string>> Decremento(int id)
        {
            var detalle = await _shopCartRepository.ItemsDelCarrito(id);

            if (detalle == null)
            {
                _logger.LogError("Item no encontrado");
                return OperationResult<string>.Fail();
            }

            var producto = await _productRepository.ObtenerProductoPorIdAsync(detalle.ProductoId);
            if (producto == null)
            {
                return OperationResult<string>.Fail("Error al recuperar el product para devolver stock");
            }

            // 1. Devolvemos el stock inmediatamente
            producto.Cantidad++;
            await _productRepository.ActualizarProductoAsync(producto);

            // 2. Gestionamos la cantidad del item
            detalle.Cantidad--;
            if (detalle.Cantidad <= 0)
            {
                await _orderRepository.EliminarDetallePedidoAsync(detalle);
            }
            else
            {
                await _orderRepository.ActualizarDetallePedidoAsync(detalle);
            }

            return OperationResult<string>.Ok("Decremento realizado con exito");
        }

        public async Task<OperationResult<string>> EliminarProductoCarrito(int id)
        {
            var detalle = await _orderRepository.ObtenerDetallePorIdAsync(id);
            if (detalle == null)
            {
                return OperationResult<string>.Fail();
            }

            var producto = await _productRepository.ObtenerProductoPorIdAsync(detalle.ProductoId);
            if (producto != null)
            {
                producto.Cantidad += detalle.Cantidad;
                await _productRepository.ActualizarProductoAsync(producto);
            }

            await _orderRepository.EliminarDetallePedidoAsync(detalle);

            return OperationResult<string>.Ok("Eliminacion exitosa del product");
        }
    }
}