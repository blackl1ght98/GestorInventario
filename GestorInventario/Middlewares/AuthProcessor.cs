using GestorInventario.Interfaces.Application;

namespace GestorInventario.Middlewares
{
    // Capa 2: Processor - Coordinación
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