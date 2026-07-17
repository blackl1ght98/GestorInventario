using GestorInventario.Interfaces.Application.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GestorInventario.Application.Services.Authentication.Strategies.Configuration
{
    public class SymmetricAuthenticationStrategy : IConfigurationAuthenticationStrategy
    {
        private readonly ILogger<SymmetricAuthenticationStrategy> _logger;

        public SymmetricAuthenticationStrategy(ILogger<SymmetricAuthenticationStrategy> logger)
        {
            _logger = logger;
        }

        public void Configure(IServiceCollection services, IConfiguration configuration)
        {
            var secret = configuration["ClaveJWT"] ?? Environment.GetEnvironmentVariable("ClaveJWT");
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogError("La clave JWT no está configurada en ClaveJWT.");
                throw new InvalidOperationException("La clave JWT es requerida.");
            }

            if (secret.Length < 32)
            {
                _logger.LogError("La clave JWT es demasiado corta. Debe tener al menos 32 caracteres.");
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
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
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