using GestorInventario.Application.Interfaces.Application.Services.BackgroundServices;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Application.Services.BackgroundServices;

public class CartCleanupService : ICartCleanupService
{
    private readonly IShopCartRepository _shopCartRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<CartCleanupService> _logger;

    public CartCleanupService(
        IShopCartRepository shopCartRepository,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ILogger<CartCleanupService> logger)
    {
        _shopCartRepository = shopCartRepository;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<int> CleanupAbandonedCartsAsync(CancellationToken ct)
    {
        // 1. Buscar carritos activos con más de 24 horas de antigüedad
        var cutoff = DateTime.UtcNow.AddDays(-1);
        var abandonedCarts = await _shopCartRepository.ObtenerCarritosAbandonadosAsync(cutoff);

        int totalRecuperados = 0;

        foreach (var carrito in abandonedCarts)
        {
            try
            {
                // 2. Obtener los items del shopCart
                var items = await _shopCartRepository.ObtenerItemsDelCarritoUsuario(carrito.Id);

                foreach (var item in items)
                {
                    // 3. Devolver el stock al product
                    var producto = await _productRepository.ObtenerProductoPorIdAsync(item.ProductoId);
                    if (producto != null)
                    {
                        producto.Cantidad += item.Cantidad;
                        await _productRepository.ActualizarProductoAsync(producto);
                    }
                }

                // 4. Eliminar el shopCart abandonado
                await _orderRepository.EliminarCarritoAsync(carrito);
                totalRecuperados++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error limpiando el shopCart {CartId}", carrito.Id);
            }
        }
        return totalRecuperados;
    }
}