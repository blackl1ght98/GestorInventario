using GestorInventario.Interfaces.Application;
using GestorInventario.MetodosExtension.Metodos_program.cs;

namespace GestorInventario.Middlewares
{



    /// <summary>
    /// Extensiones para configurar el middleware de procesamiento de autenticación.
    /// 
    /// Permite inyectar diferentes estrategias de autenticación (dinámica, fija, simétrica, etc.)
    /// de forma centralizada en la pipeline de ASP.NET Core.
    /// </summary>
    public static  class AuthProcessingExtensions
    {
        // Metodo que recibe el modo de autenticacion elegido.
        public static IApplicationBuilder UseAuthProcessing(this IApplicationBuilder app, string authStrategy)
        {
            // 1º Llama a la clase fabrica y le pasa el metodo de autenticacion elegido
            IAuthProcessingStrategy strategy = AuthenticationProcessingFactory.CreateAuthProcessingStrategy(authStrategy);
            // 2º Segun el metodo de autenticacion escogido el orquestador ejecutara la configuracion correspondiente
            var processor = new AuthProcessor(strategy);

            app.Use(async (httpContext, next) =>
            {
                await processor.ExecuteAuthentication(httpContext, next);
            });

            return app;
        }
    }
}

