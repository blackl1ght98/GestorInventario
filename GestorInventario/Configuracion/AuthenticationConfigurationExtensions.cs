

using GestorInventario.Interfaces.Application.Authentication;

namespace GestorInventario.Configuracion
{

    /**
     El motivo por el cual no se ha seguido el mismo patron para quitar esta logica
    y sustituirla por un resolver aqui en este caso el resolver no aporta nada y como
    esta logica se ejecuta al inicio el contenedor de dependencias no esta "vivo" ese 
    contenedor vive cuando haces  builder.Build(); a partir de esta linea es cuando vive 
     
     */
    public static class AuthenticationConfigurationExtensions
    {
        public const string AuthModeConfigKey = "AuthMode";

        public static IServiceCollection AddConfigureAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var mode = configuration[AuthModeConfigKey] ?? "AsymmetricDynamic";

       
            var loggerFactory = LoggerFactory.Create(b => b.AddConsole());

          
            IConfigurationAuthenticationStrategy Build<TStrategy>(
                Func<ILoggerFactory, TStrategy> factory)
                where TStrategy : IConfigurationAuthenticationStrategy
            {
                var instance = factory(loggerFactory);
                instance.Configure(services, configuration);
                return instance;
            }

            switch (mode)
            {
                case "Symmetric":
                    Build(logger => new SymmetricAuthenticationStrategy(
                        logger.CreateLogger<SymmetricAuthenticationStrategy>()));
                    break;

                case "AsymmetricFixed":
                    Build(logger => new AsymmetricFixedAuthenticationStrategy(
                        logger.CreateLogger<AsymmetricFixedAuthenticationStrategy>()));
                    break;

                case "AsymmetricDynamic":
                    Build(logger => new AsymmetricDynamicAuthenticationStrategy(
                        logger.CreateLogger<AsymmetricDynamicAuthenticationStrategy>()));
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Modo de autenticación no soportado: {mode}. " +
                        $"Valores válidos: Symmetric, AsymmetricFixed, AsymmetricDynamic.");
            }

            return services;
        }
    }
}