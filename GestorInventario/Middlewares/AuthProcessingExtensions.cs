using GestorInventario.Interfaces.Application;
using GestorInventario.MetodosExtension.Metodos_program.cs;

namespace GestorInventario.Middlewares
{
    /// <summary>
    /// Extensión encargada de agregar al pipeline un middleware de autenticación.
    /// Selecciona la estrategia de autenticación indicada, crea un procesador
    /// y delega en él la ejecución del proceso de autenticación por solicitud.
    /// </summary>

    public static  class AuthProcessingExtensions
    {
   
        public static IApplicationBuilder UseAuthProcessing(this IApplicationBuilder app, string authStrategy)
        {
            IAuthProcessingStrategy strategy = StrategyFactory.CreateAuthProcessingStrategy(authStrategy);

            var processor = new AuthProcessor(strategy);

            app.Use(async (httpContext, next) =>
            {
                await processor.ExecuteAuthentication(httpContext, next);
            });

            return app;
        }
    }
}

