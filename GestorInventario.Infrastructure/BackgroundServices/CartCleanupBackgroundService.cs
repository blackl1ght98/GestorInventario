using GestorInventario.Application.Interfaces.Application.Services.BackgroundServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Infrastructure.BackgroundServices;
public class CartCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CartCleanupBackgroundService> _logger;
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

    public CartCleanupBackgroundService(IServiceProvider serviceProvider, ILogger<CartCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CartCleanupBackgroundService iniciado.");
        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var cleanupService = scope.ServiceProvider.GetRequiredService<ICartCleanupService>();

                int eliminados = await cleanupService.CleanupAbandonedCartsAsync(stoppingToken);

                if (eliminados > 0)
                    _logger.LogInformation("CartCleanupBackgroundService: {Count} carritos abandonados limpiados y stock recuperado.", eliminados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CartCleanupBackgroundService.");
            }

            await Task.Delay(Intervalo, stoppingToken);
        }
    }
}