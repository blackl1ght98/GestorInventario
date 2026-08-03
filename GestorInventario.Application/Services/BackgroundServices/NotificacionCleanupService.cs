using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Application.Services.BackgroundServices
{
    public class NotificacionCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificacionCleanupService> _logger;

        public NotificacionCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificacionCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificacionCleanupService iniciado.");

            // Espera inicial: deja que la app termine de arrancar
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider
                        .GetRequiredService<INotificationRepository>();

                    var cutoff = DateTime.UtcNow.AddDays(-90);
                    int eliminadas = await repository
                        .EliminarNotificacionesAntiguas(cutoff);

                    if (eliminadas > 0)
                    {
                        _logger.LogInformation(
                            "NotificacionCleanupService: {Count} notificación(es) eliminada(s) (cutoff={Cutoff:yyyy-MM-dd}).",
                            eliminadas, cutoff);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Apagado normal de la app
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "NotificacionCleanupService: error durante la limpieza.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("NotificacionCleanupService detenido.");
        }
    }
}
