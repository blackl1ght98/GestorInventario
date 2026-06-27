using GestorInventario.Application.Services.Authentication.Strategies;
using GestorInventario.Application.Services.Authentication.Token_generation;
using GestorInventario.Infraestructure.Repositories.UserRepository;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;

namespace GestorInventario.Application.Services.Authentication
{
    public static class TokenStrategyServiceCollectionExtensions
    {
        public const string AuthModeConfigKey = "AuthMode";

        public static IServiceCollection AddTokenStrategies(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddSingleton<TokenClaimsBuilder>();
           

            var mode = configuration[AuthModeConfigKey] ?? "AsymmetricDynamic";

            services.AddScoped<ITokenStrategy>(sp =>
            {
                var claimsBuilder = sp.GetRequiredService<TokenClaimsBuilder>();
                return mode switch
                {
                    "Symmetric" => new SymmetricTokenStrategy(configuration, claimsBuilder),

                    "AsymmetricFixed" => new AsymmetricFixedTokenStrategy(configuration, claimsBuilder),

                    "AsymmetricDynamic" => new AsymmetricDynamicTokenStrategy(
                        configuration,
                        claimsBuilder,
                        sp.GetRequiredService<ICacheService>(),
                        sp.GetRequiredService<ILogger<AsymmetricDynamicTokenStrategy>>()),

                    _ => throw new InvalidOperationException(
                            $"Modo de autenticación no soportado: {mode}.")
                };
            });

            services.AddScoped<IRefreshTokenStrategy>(sp =>
            {
                var claimsBuilder = sp.GetRequiredService<TokenClaimsBuilder>();
                return mode switch
                {
                    "Symmetric" => new RefreshSymmetricToken(
                        claimsBuilder, configuration,
                        sp.GetRequiredService<ILogger<RefreshSymmetricToken>>()),

                    "AsymmetricFixed" => new RefreshAsymetricFixedToken(
                        claimsBuilder, configuration),

                    "AsymmetricDynamic" => new RefreshAsymmetricDynamicToken(
                        claimsBuilder, sp.GetRequiredService<ICacheService>()),

                    _ => throw new InvalidOperationException(
                            $"Modo de autenticación no soportado: {mode}.")
                };
            });

            services.AddScoped<ITokenGenerator, TokenGenerator>();
            services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();

            return services;
        }
    }
}
