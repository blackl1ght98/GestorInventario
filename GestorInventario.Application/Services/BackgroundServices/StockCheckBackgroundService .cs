using GestorInventario.Interfaces.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Application.Services.BackgroundServices
{
    /// <summary>
    /// Servicio en segundo plano encargado de monitorear el stock de productos
    /// y notificar a los administradores cuando los niveles son críticos.
    /// </summary>
    public class StockCheckBackgroundService : BackgroundService
    {
        // Intervalo de ejecución recurrente (por defecto 1 hora)
        private static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(1);

        // Retraso inicial para permitir que la aplicación termine de arrancar antes de la primera ejecución
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(1);

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StockCheckBackgroundService> _logger;
        private readonly TimeSpan _intervalo;

        public StockCheckBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<StockCheckBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

          
            _intervalo = DefaultInterval;

            _logger.LogWarning("Servicio de Stock configurado con un intervalo de: {Intervalo}", _intervalo);
        }

        /// <summary>
        /// Método principal ejecutado por el Host de .NET al iniciar la aplicación.
        /// </summary>
        /// <param name="stoppingToken">Token que indica cuándo debe detenerse el servicio (ej. al apagar la app).</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Iniciando monitoreo de stock. Intervalo: {Intervalo}. Delay inicial: {Delay}",
                _intervalo, StartupDelay);

            // 1. Evitamos colisiones durante el arranque de la aplicación
            await Task.Delay(StartupDelay, stoppingToken);

            // Bucle de ejecución persistente hasta que el Host solicite la detención mediante el token
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Ejecutando verificación programada de stock...");

                try
                {
                    /*
                     * GESTIÓN DE SCOPES (Ámbitos de Servicio):
                     * BackgroundService es un Singleton (vive toda la vida de la app).
                     * Los repositorios y servicios de negocio suelen ser Scoped (viven solo una petición).
                     *
                     * No podemos inyectar un servicio Scoped directamente en el constructor de un Singleton.
                     * Solución: Inyectamos IServiceProvider y creamos un Scope manual. Esto simula
                     * el ciclo de vida de una petición HTTP, asegurando que el DbContext se cree
                     * y se destruya correctamente en cada iteración.
                     */
                    using var scope = _serviceProvider.CreateScope();

                    // Resolvemos el servicio dentro del scope creado
                    var stockService = scope.ServiceProvider
                        .GetRequiredService<IStockCheckService>();

                    /*
                     * PROPAGACIÓN DEL CANCELLATION TOKEN:
                     * Pasamos 'stoppingToken' hacia abajo en la cadena de llamadas.
                     * Esto permite que si la aplicación se apaga mientras el servicio está
                     * consultando la DB o enviando emails, estas tareas se cancelen inmediatamente
                     * evitando procesos huérfanos o consumo innecesario de recursos.
                     */
                    await stockService.VerificarYNotificarStockBajoAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                   
                    _logger.LogInformation("La tarea de verificación fue cancelada por el sistema.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error crítico durante la ejecución del servicio de stock.");
                }

                _logger.LogInformation("Próxima verificación en {Intervalo}", _intervalo);

                // Espera asíncrona hasta el siguiente ciclo.
                // El token permite interrumpir esta espera inmediatamente si la app se apaga.
                await Task.Delay(_intervalo, stoppingToken);
            }

            _logger.LogInformation("Servicio de verificación de stock detenido completamente.");
        }
    }
}