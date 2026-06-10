using GestorInventario.Configuracion;
using GestorInventario.Interfaces.Application;

namespace GestorInventario.MetodosExtension
{
    public static  class ConfigAuthentication
    {
        public static IServiceCollection ConfigureAuthentication(this IServiceCollection services,IConfiguration configuration, string authStrategy)
        {
            IConfigurationAuthenticationStrategy strategy = AuthenticationConfigurationStrategyFactory.Create(authStrategy);

            var configurator = new AuthenticationConfigurator(strategy);
            configurator.Configure(services, configuration);
            return services;
        }
    }
}
