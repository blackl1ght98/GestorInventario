using GestorInventario.Interfaces.Application;

namespace GestorInventario.Middlewares
{
    public static  class AuthProcessingExtensions
    {
        public static IApplicationBuilder UseAuthProcessing(this IApplicationBuilder app, WebApplicationBuilder builder, IAuthProcessingStrategy strategy)
        {
            var processor = new AuthProcessor(strategy);

            app.Use(async (httpContext, next) =>
            {
                await processor.ExecuteAuthentication(httpContext, builder, next);
            });

            return app;
        }
    }
}

