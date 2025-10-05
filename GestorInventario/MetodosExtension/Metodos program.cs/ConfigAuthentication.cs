using GestorInventario.Configuracion;
using GestorInventario.Configuracion.Strategies;
using GestorInventario.Interfaces.Application;

namespace GestorInventario.MetodosExtension.Metodos_program.cs
{
    public static  class ConfigAuthentication
    {
        public static IServiceCollection ConfigureAuthentication(this IServiceCollection services,IConfiguration configuration, string authStrategy)
        {
            IAuthenticationStrategy strategy = StrategyFactory.CreateAuthenticationStrategy(authStrategy);

            var configurator = new AuthenticationConfigurator(strategy);
            configurator.Configure(services, configuration);
            return services;
        }
    }
}
