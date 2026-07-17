using GestorInventario.Interfaces.Application.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies.Configuration
{
    public class AsymmetricFixedAuthenticationStrategy : IConfigurationAuthenticationStrategy
    {
        private readonly ILogger<AsymmetricFixedAuthenticationStrategy> _logger;

        public AsymmetricFixedAuthenticationStrategy(ILogger<AsymmetricFixedAuthenticationStrategy> logger)
        {
            _logger = logger;
        }

        public void Configure(IServiceCollection services, IConfiguration configuration)
        {
            var publicKey = configuration["Jwt:PublicKey"] ?? Environment.GetEnvironmentVariable("PUBLIC_KEY");
            if (string.IsNullOrEmpty(publicKey))
            {
                _logger.LogError("La clave pública JWT no está configurada en Jwt:PublicKey.");
                throw new InvalidOperationException("La clave pública JWT es requerida.");
            }

            RSA rsa;
            try
            {
                rsa = RSA.Create();   
                rsa.FromXmlString(publicKey);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex,
                    "La clave pública no es XML RSA válido. Contenido recibido: {Key}",
                    publicKey);
                throw new InvalidOperationException("La clave pública RSA es inválida.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al cargar la clave pública RSA.");
                throw;
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
                        _logger.LogInformation("Redirigiendo al login desde AddCookie para {Path}",
                            context.Request.Path);
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
                    ValidIssuer = configuration["JwtIssuer"] ?? Environment.GetEnvironmentVariable("JWT_ISSUER"),
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["auth"];
                        _logger.LogInformation("Token extraído de cookie 'auth' para {Path}",
                            context.Request.Path);
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        _logger.LogWarning(context.Exception, "Fallo JWT en {Path}", context.Request.Path);
                        return Task.CompletedTask;
                    }
                };
            });
        }
    }
}