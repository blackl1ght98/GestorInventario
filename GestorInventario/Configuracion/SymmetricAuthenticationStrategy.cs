using GestorInventario.Interfaces.Application;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GestorInventario.Configuracion.Strategies
{
    public class SymmetricAuthenticationStrategy : IAuthenticationStrategy
    {
        public IServiceCollection ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            var loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<SymmetricAuthenticationStrategy>();

            var secret = configuration["ClaveJWT"] ?? Environment.GetEnvironmentVariable("ClaveJWT");
            if (string.IsNullOrEmpty(secret))
            {
                logger.LogError("La clave JWT no está configurada en ClaveJWT.");
                throw new InvalidOperationException("La clave JWT es requerida.");
            }

            if (secret.Length < 32)
            {
                logger.LogError("La clave JWT es demasiado corta. Debe tener al menos 32 caracteres.");
                throw new InvalidOperationException("La clave JWT es demasiado corta.");
            }

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
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout"; 
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        logger.LogInformation("Redirigiendo al login desde AddCookie para la ruta {Path}", context.Request.Path);
                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    }
                };
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JwtIssuer"] ?? Environment.GetEnvironmentVariable("JwtIssuer"),
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JwtAudience"),
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["auth"];
                        logger.LogInformation("Token extraído de la cookie 'auth' para la ruta {Path}", context.Request.Path);
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        logger.LogWarning(context.Exception, "Fallo en la autenticación JWT para la ruta {Path}", context.Request.Path);
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }
    }
}