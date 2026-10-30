using GestorInventario.Application.Interfaces.Infrastructure.Common;
using GestorInventario.Domain.enums.Pedido;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class LimpiezaPedidosExpiradosService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LimpiezaPedidosExpiradosService> _logger;

    public LimpiezaPedidosExpiradosService(
        IServiceProvider serviceProvider, 
        ILogger<LimpiezaPedidosExpiradosService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de limpieza de pedidos pendientes iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    // Como los BackgroundService son Singleton y los DbContext suelen ser Scoped,
                    // creamos un scope manual para resolver el repositorio.
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                   
                    var limiteTiempo = DateTime.UtcNow.AddHours(-24);
                    // Método que busca todos los pedidos en estado Pendiente con más de 24h en todo el sistema
                    var pedidosExpirados = await unitOfWork.OrderRepository.ObtenerTodosLosPendientesAntiguosAsync(limiteTiempo);

                    foreach (var pedido in pedidosExpirados)
                    {
                        pedido.EstadoPedido = EstadoPedido.Cancelado.ToString();
                        await unitOfWork.OrderRepository.ActualizarPedidoAsync(pedido);

                        foreach (var detalle in pedido.DetallePedidos)
                        {
                            var producto = await unitOfWork.ProductRepository.ObtenerProductoPorIdAsync(detalle.ProductoId);
                            if (producto != null)
                            {
                                producto.Cantidad += detalle.Cantidad;
                                await unitOfWork.ProductRepository.ActualizarProductoAsync(producto);
                            }
                        }
                        
                        _logger.LogInformation("Pedido #{Id} expirado en segundo plano. Stock devuelto.", pedido.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al limpiar pedidos pendientes expirados en segundo plano.");
            }

            // Esperar 1 hora antes de volver a ejecutar la comprobación
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}