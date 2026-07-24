using GestorInventario.Application.Services.Authentication.Strategies.Configuration;
using GestorInventario.Interfaces.Application.Services.Authentication;

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
            
            using var compositionProvider = services.BuildServiceProvider();

            IConfigurationAuthenticationStrategy Build<TStrategy>()
                where TStrategy : IConfigurationAuthenticationStrategy
            {
                var instance = ActivatorUtilities.CreateInstance<TStrategy>(compositionProvider);
                instance.Configure(services, configuration);
                return instance;
            }

            switch (mode)
            {
                case "Symmetric":
                    Build<SymmetricAuthenticationStrategy>();
                    break;

                case "AsymmetricFixed":
                    Build<AsymmetricFixedAuthenticationStrategy>();
                    break;

                case "AsymmetricDynamic":
                    Build<AsymmetricDynamicAuthenticationStrategy>();
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