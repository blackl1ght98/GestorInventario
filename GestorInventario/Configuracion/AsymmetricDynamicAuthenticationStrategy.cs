using Microsoft.AspNetCore.Authentication.Cookies;
using GestorInventario.Interfaces.Application;

namespace GestorInventario.Configuracion.Strategies
{
    public class AsymmetricDynamicAuthenticationStrategy : IAuthenticationStrategy
    {
        public IServiceCollection ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.SlidingExpiration = true;
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        var loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
                        var logger = loggerFactory.CreateLogger<AsymmetricDynamicAuthenticationStrategy>();
                        logger.LogInformation("Redirigiendo al login desde AddCookie");
                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }
    }
}