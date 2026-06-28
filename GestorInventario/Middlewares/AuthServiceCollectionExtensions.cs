using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Middlewares.Strategis;
using GestorInventario.Shared.Auth.Strategies;

namespace GestorInventario.Middlewares
{
    public static class AuthServiceCollectionExtensions
    {
        public const string AuthModeConfigKey = "AuthMode";

        /// <summary>
        /// Registra la estrategia de autenticación y sus dependencias.
     
        /// Modos soportados: "Symmetric", "AsymmetricFixed", "AsymmetricDynamic".
        /// </summary>
        public static IServiceCollection AddJwtAuth(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var mode = configuration[AuthModeConfigKey] ?? "AsymmetricDynamic";

            services.AddScoped<IAuthenticationMiddlewareStrategy>(sp =>
            {
                return mode switch
                {
                    "Symmetric" => new SymmetricAuthStrategy(
                                                configuration,
                                                sp.GetRequiredService<ITokenGenerator>(),
                                                sp.GetRequiredService<IUserRepository>(),
                                                sp.GetRequiredService<IRefreshTokenStrategy>()
                                                ),

                    "AsymmetricFixed" => new FixedAsymmetricAuthStrategy(
                                                configuration,
                                                sp.GetRequiredService<ITokenGenerator>(),
                                                sp.GetRequiredService<IUserRepository>(),
                                                sp.GetRequiredService<IRefreshTokenStrategy>()),

                    "AsymmetricDynamic" => new DynamicAsymmetricAuthStrategy(
                                                configuration,
                                                sp.GetRequiredService<ITokenGenerator>(),
                                                sp.GetRequiredService<IUserRepository>(),
                                                sp.GetRequiredService<ICacheService>(),
                                                sp.GetRequiredService<ILogger<DynamicAsymmetricAuthStrategy>>()),

                    _ => throw new InvalidOperationException(
                            $"Modo de autenticación no soportado: {mode}. " +
                            $"Valores válidos: Symmetric, AsymmetricFixed, AsymmetricDynamic.")
                };
            });

            return services;
        }
    }
}