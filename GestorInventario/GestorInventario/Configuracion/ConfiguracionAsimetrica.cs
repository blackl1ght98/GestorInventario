using GestorInventario.Application.Services;
using GestorInventario.Interfaces.Application;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GestorInventario.Configuracion
{
    public static class ConfiguracionClaveAsimetrica
    {
        public static IServiceCollection ConfiguracionAsimetricaV1(this WebApplicationBuilder builder, IConfiguration configuration)
        {
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.SlidingExpiration = true;
            })
            .AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var httpContextAccessor = context.HttpContext.RequestServices.GetRequiredService<IHttpContextAccessor>();
                        var tokenService = context.HttpContext.RequestServices.GetRequiredService<ITokenGenerator>();
                        var memoryCache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

                        var token = context.Request.Cookies["auth"];
                        if (string.IsNullOrEmpty(token))
                        {
                            context.Response.Cookies.Delete("auth");
                            if (context.Request.Path != "/Auth/Login")
                            {
                                context.Response.Redirect("/Auth/Login");
                            }
                            return Task.CompletedTask;
                        }

                        var userId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                        if (string.IsNullOrEmpty(userId))
                        {
                            context.Response.Cookies.Delete("auth");
                            if (context.Request.Path != "/Auth/Login")
                            {
                                context.Response.Redirect("/Auth/Login");
                            }
                            return Task.CompletedTask;
                        }

                        memoryCache.TryGetValue(userId + "PublicKey", out byte[] publicKeyCifrada);
                        if (publicKeyCifrada == null)
                        {
                            context.Response.Cookies.Delete("auth");
                            if (context.Request.Path != "/Auth/Login")
                            {
                                context.Response.Redirect("/Auth/Login");
                            }
                            return Task.CompletedTask;
                        }

                        // Configura la clave pública desde el caché
                        var publicKey = Encoding.UTF8.GetString(publicKeyCifrada);

                        var rsa = RSA.Create();
                        rsa.FromXmlString(publicKey);

                        // Configura las opciones para la validación del token JWT.
                        context.Options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = builder.Configuration["JwtIssuer"] ?? Environment.GetEnvironmentVariable("JwtIssuer"),
                            ValidateAudience = true,
                            ValidAudience = builder.Configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JwtAudience"),
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,

                            // Establece la clave de firma del emisor
                            IssuerSigningKey = new RsaSecurityKey(rsa)
                        };

                        context.Token = token;
                        return Task.CompletedTask;
                    }
                };
            });

            return builder.Services;
        }
    }
}
