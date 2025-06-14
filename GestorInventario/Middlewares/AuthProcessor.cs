using GestorInventario.Interfaces.Application;

namespace GestorInventario.Middlewares
{
    public class AuthProcessor
    {
        private IAuthProcessingStrategy _strategy;

        public AuthProcessor(IAuthProcessingStrategy strategy)
        {
            _strategy = strategy;
        }

        public void SetStrategy(IAuthProcessingStrategy strategy)
        {
            _strategy = strategy;
        }

        public async Task ExecuteAuthentication(HttpContext context, WebApplicationBuilder builder, Func<Task> next)
        {
            await _strategy.ProcessAuthentication(context, builder, next);
        }
    }
}