using GestorInventario.Interfaces.Application;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace GestorInventario.Configuracion.Strategies
{
    public class AsymmetricFixedAuthenticationStrategy : IAuthenticationStrategy
    {
        public IServiceCollection ConfigureAuthentication(WebApplicationBuilder builder, IConfiguration configuration)
        {
            var loggerFactory = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<AsymmetricFixedAuthenticationStrategy>();

            var publicKey = configuration["Jwt:PublicKey"] ?? Environment.GetEnvironmentVariable("PublicKey");
            if (string.IsNullOrEmpty(publicKey))
            {
                logger.LogError("La clave pública JWT no está configurada en Jwt:PublicKey.");
                throw new InvalidOperationException("La clave pública JWT es requerida.");
            }

            var rsa = new RSACryptoServiceProvider();
            try
            {
                rsa.FromXmlString(publicKey);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al cargar la clave pública RSA desde la configuración.");
                throw new InvalidOperationException("La clave pública RSA es inválida.", ex);
            }

            builder.Services.AddAuthentication(options =>
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
                    IssuerSigningKey = new RsaSecurityKey(rsa)
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

            return builder.Services;
        }
    }
}