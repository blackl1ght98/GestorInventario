using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Infrastructure.Authentication.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Infrastructure.Configuration
{
    public static class AuthenticationConfigurationExtensions
    {
        public static IServiceCollection AddConfigureAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var envConfig = new EnvironmentConfig(configuration);

            // Seleccionamos la estrategia según el modo de autenticación
            IConfigurationAuthenticationStrategy strategy = envConfig.AuthMode switch
            {
                "Symmetric" => new SymmetricAuthenticationStrategy(),
                "AsymmetricFixed" => new AsymmetricFixedAuthenticationStrategy(),
                "AsymmetricDynamic" => new AsymmetricDynamicAuthenticationStrategy(),
                _ => throw new InvalidOperationException(
                    $"Modo de autenticación no soportado: {envConfig.AuthMode}. " +
                    $"Valores válidos: Symmetric, AsymmetricFixed, AsymmetricDynamic.")
            };

            // Ejecutamos la configuración de la estrategia elegida
            strategy.Configure(services, configuration);

            return services;
        }
    }
}