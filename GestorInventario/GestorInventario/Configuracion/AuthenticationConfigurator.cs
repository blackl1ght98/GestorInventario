using GestorInventario.Interfaces.Application;

namespace GestorInventario.Configuracion
{
    public class AuthenticationConfigurator
    {
        private readonly IAuthenticationStrategy _strategy;

        public AuthenticationConfigurator(IAuthenticationStrategy strategy)
        {
            _strategy = strategy;
        }

        public IServiceCollection Configure(WebApplicationBuilder builder, IConfiguration configuration)
        {
            return _strategy.ConfigureAuthentication(builder, configuration);
        }
    }
}