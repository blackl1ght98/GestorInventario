using GestorInventario.Interfaces.Application;

namespace GestorInventario.Middlewares
{
    /// <summary>
    /// Coordina el proceso de autenticación aplicando la estrategia seleccionada.
    /// Su responsabilidad es ejecutar el flujo de autenticación por solicitud,
    /// delegando toda la lógica específica a la implementación de estrategia inyectada.
    /// </summary>

    public class AuthProcessor
    {
        private IAuthProcessingStrategy _strategy;

        public AuthProcessor(IAuthProcessingStrategy strategy)
        {
            _strategy = strategy;
        }
        public async Task ExecuteAuthentication(HttpContext context, Func<Task> next)
        {
            // Responsabilidad: ORQUESTAR el proceso de autenticación
            await _strategy.ProcessAuthentication(context, next);
        }
    }
}