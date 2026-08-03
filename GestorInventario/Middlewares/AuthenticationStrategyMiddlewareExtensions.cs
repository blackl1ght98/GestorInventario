using GestorInventario.Interfaces.Application;


namespace GestorInventario.Middlewares
{



  
    public static  class AuthenticationStrategyMiddlewareExtensions
    {
      
        public static IApplicationBuilder UseJwtAuthStrategy(this IApplicationBuilder app)
        {
            return app.UseMiddleware<AuthenticationStrategyMiddleware>();
        }
    }
}

