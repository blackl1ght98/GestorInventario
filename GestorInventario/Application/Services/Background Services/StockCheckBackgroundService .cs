using GestorInventario.Interfaces.Application.Services;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class StockCheckBackgroundService : BackgroundService
    {
        // Constante: intervalo por defecto si no hay config (1 hora)
        private static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(1);

        // Constante: delay inicial tras arranque para no competir con startup (1 minuto)
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
            _logger.LogWarning("Valor del intervalo: {Intervalo}", _intervalo);
        }

        /**
           * CancellationToken es una señal de cooperación que el Host de ASP.NET Core
           * envía cuando la aplicación se está deteniendo (cierre controlado, redeploy,
           * reinicio del servidor, escalado en la nube, Docker enviando SIGTERM, etc.).
           * 
           * Flujo de propagación (cascada):
           * IHost (ASP.NET) -> ExecuteAsync(stoppingToken) -> VerificarYNotificarStockBajoAsync(stoppingToken)
           * -> ObtenerEmailsAdministradoresAsync(stoppingToken) -> ToListAsync(stoppingToken)
           * 
           * ¿Por qué CancellationToken se propaga por toda la cadena?
           * Porque si el Host dice "para ya", cada eslabón debe poder abortar su trabajo de forma limpia.
           * Si en algún eslabón ignoramos el token, la señal muere ahí y el código de abajo sigue
           * trabajando ciego, consumiendo memoria y dejando procesos huérfanos.
           * 
           * ¿Por qué no inyectamos IStockNotificationService directamente en el constructor?
           * 
           * Porque BackgroundService es un SINGLETON (vive toda la vida de la app), pero los repositorios
           * y servicios que usa son SCOPED (viven solo durante una petición HTTP o un scope manual).
           * Si inyectáramos un scoped service en un singleton, EF Core lanzaría:
           * "Cannot resolve scoped service from root provider".
           * 
           * Solución: no inyectamos el servicio en el constructor. En su lugar, inyectamos
           * IServiceProvider (que sí es Singleton) y dentro del bucle creamos un scope manual:
           *   using var scope = _serviceProvider.CreateScope();
           * Esto simula una petición HTTP: crea instancias scoped frescas, las usa y las destruye
           * al cerrar el 'using', liberando DbContext y conexiones.
   */
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Servicio de verificación de stock iniciado. " +
                "Intervalo configurado: {Intervalo}. Delay inicial: {Delay}",
                _intervalo, StartupDelay);

            // Esperamos 1 minuto antes de la primera ejecución para no competir con el arranque de la app.
            // Si durante esta espera el servidor se apaga (stoppingToken se activa), Task.Delay se interrumpe
            // inmediatamente y salimos sin ejecutar nada.
            await Task.Delay(StartupDelay, stoppingToken);

            // Bucle infinito que solo se rompe cuando el Host solicita la detención.
          
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Ejecutando verificación programada de stock...");

                try
                {
                    // Creamos un scope de inyección de dependencias manual.
                    // Esto simula una "petición HTTP" para que los servicios scoped funcionen dentro de un singleton.
                    // El 'using' garantiza que se liberen recursos (DbContext, conexiones, etc.) al terminar.
                    using var scope = _serviceProvider.CreateScope();

                    // Resolvemos el servicio desde el scope, no desde el constructor.
                    var stockService = scope.ServiceProvider
                        .GetRequiredService<IStockNotificationService>();

                    // Pasamos el token para que, si el servidor se apaga mientras envía 50 emails,
                    // el servicio pueda abortar en el producto 20 y no seguir ciego hasta el 50.
                    await stockService.VerificarYNotificarStockBajoAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en la ejecución del servicio de stock.");
                }

                _logger.LogInformation(
                    "Próxima verificación de stock en {Intervalo}",
                    _intervalo);

                // Dormimos el tiempo configurado. Si el token se activa durante la siesta,
                // Task.Delay lanza OperationCanceledException y salimos del while limpiamente.
                await Task.Delay(_intervalo, stoppingToken);
            }

            _logger.LogInformation("Servicio de verificación de stock detenido.");
        }
    }
}