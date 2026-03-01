using GestorInventario.Interfaces.Application;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; 
                options.Cookie.SameSite = SameSiteMode.Lax;  
                options.Cookie.IsEssential = true;           // Necesario para GDPR/funcionalidad básica

                // Rutas de login/logout (consistente con tu proyecto)
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.AccessDeniedPath = "/Auth/AccessDenied"; 

                // Tiempo de vida y renovación
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60); 
                options.SlidingExpiration = true;                

                // Eventos útiles para depuración y redirecciones limpias
                options.Events = new CookieAuthenticationEvents
                {
                    // Redirigir a login cuando se necesita autenticación
                    OnRedirectToLogin = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<AsymmetricDynamicAuthenticationStrategy>>();
                        logger.LogInformation("Redirigiendo a login desde cookie: {RedirectUri}", context.RedirectUri);
                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    },

                    // Opcional: log cuando se rechaza principal (útil para debug)
                    OnValidatePrincipal = context =>
                    {
                        if (context.Principal?.Identity?.IsAuthenticated != true)
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<AsymmetricDynamicAuthenticationStrategy>>();
                            logger.LogWarning("Principal inválido detectado en validación de cookie");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

     

            return services;
        }
    }
}