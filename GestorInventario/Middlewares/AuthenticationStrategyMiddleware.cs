using GestorInventario.Interfaces.Application;

namespace GestorInventario.Middlewares
{



    /*
      Coordina el proceso de autenticación aplicando la estrategia seleccionada.
     Su responsabilidad es ejecutar el flujo de autenticación por solicitud,
     delegando toda la lógica específica a la implementación de estrategia inyectada.
     
     */
    public class AuthenticationStrategyMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationStrategyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IAuthenticationMiddlewareStrategy strategy)
        {
            await strategy.ProcessAuthentication(context, () => _next(context));
        }
    }
}