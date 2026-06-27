using GestorInventario.Interfaces.Application;


namespace GestorInventario.Middlewares
{



  
    public static  class AuthenticationStrategyMiddlewareExtensions
    {
        // Metodo que recibe el modo de autenticacion elegido.
        /// <summary>
        /// Inserta el middleware de autenticación en el pipeline.
        /// La estrategia concreta se elige en DI según "Auth:Mode" del appsettings.
        /// </summary>
        public static IApplicationBuilder UseJwtAuthStrategy(this IApplicationBuilder app)
        {
            return app.UseMiddleware<AuthenticationStrategyMiddleware>();
        }
    }
}

