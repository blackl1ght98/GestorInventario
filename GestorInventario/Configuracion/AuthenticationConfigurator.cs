using GestorInventario.Interfaces.Application;

namespace GestorInventario.Configuracion
{
    public class AuthenticationConfigurator
    {
        private readonly IConfigurationAuthenticationStrategy _strategy;

        public AuthenticationConfigurator(IConfigurationAuthenticationStrategy strategy)
        {
            _strategy = strategy;
        }

        public IServiceCollection Configure(IServiceCollection services, IConfiguration configuration)
        {
            return _strategy.ConfigureAuthentication(services, configuration);
        }
    }
}