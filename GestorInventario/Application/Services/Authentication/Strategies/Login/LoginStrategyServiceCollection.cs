using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.Services;

namespace GestorInventario.Application.Services.Authentication.Strategies.Login
{
    public static class LoginStrategyServiceCollection
    {
        public const string AuthModeConfigKey = "LoginMode";
        public static IServiceCollection AddLogin(
           this IServiceCollection services,
           IConfiguration configuration)
        {
            services.AddSingleton<TokenClaimsBuilder>();


            var mode = configuration[AuthModeConfigKey] ?? "MfaLogin";

            services.AddScoped<ILoginStrategy>(sp =>
            {
                var authService = sp.GetRequiredService<IAuthService>();
                var policyExecutor= sp.GetRequiredService<IPolicyExecutor>();
                var cacheService = sp.GetRequiredService<ICacheService>();
                var emailService= sp.GetRequiredService<IEmailService>();
                return mode switch
                {
                    "StandardLogin" => new StandardLoginStrategy(authService,policyExecutor),

                    "MfaLogin" => new MfaLoginStrategy(authService,policyExecutor,cacheService,emailService),

                   

                    _ => throw new InvalidOperationException(
                            $"Modo de autenticación no soportado: {mode}.")
                };

            });
            return services;
        }
    }
}
