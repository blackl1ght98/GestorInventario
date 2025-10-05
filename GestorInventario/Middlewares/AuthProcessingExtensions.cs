using GestorInventario.Interfaces.Application;
using GestorInventario.MetodosExtension.Metodos_program.cs;

namespace GestorInventario.Middlewares
{
    // Capa 1: Extensión del Middleware - Solo configuración
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

