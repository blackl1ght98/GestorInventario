using GestorInventario.Application.Services;
using GestorInventario.Interfaces.Application;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GestorInventario.Configuracion
{
    public static class ConfiguracionClaveAsimetricaDinamica
    {
        public static IServiceCollection ConfiguracionAsimetricaDinamica(this WebApplicationBuilder builder, IConfiguration configuration)
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
                        // Variable que almacena todas las cookies
                        var collectioncookies = context.Request.Cookies;
                        // Variables para acceder a servicios
                        var httpContextAccessor = context.HttpContext.RequestServices.GetRequiredService<IHttpContextAccessor>();
                        var tokenservice = context.HttpContext.RequestServices.GetRequiredService<ITokenGenerator>();
                        var memoryCache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

                        try
                        {
                            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                            // Si no hay userId o claves necesarias, eliminar cookies y redirigir
                            if (userId == null || !ValidateKeys(memoryCache, userId, collectioncookies))
                            {
                                RedirectToLogin(context, collectioncookies);
                                return Task.CompletedTask;
                            }

                            // Obtener claves
                            memoryCache.TryGetValue(userId + "PublicKey", out byte[] publicKeyCifrada);
                            memoryCache.TryGetValue(userId.ToString() + "EncryptedAesKey", out byte[] claveCifrado);

                            if (claveCifrado == null || publicKeyCifrada == null)
                            {
                                RedirectToLogin(context, collectioncookies);
                                return Task.CompletedTask;
                            }

                            // Descifrar la clave pública
                            var publicKey = Encoding.UTF8.GetString(tokenservice.Descifrar(publicKeyCifrada, claveCifrado));

                            // Convertir la clave pública a formato RSA
                            var rsa = new RSACryptoServiceProvider();
                            rsa.FromXmlString(publicKey);

                            // Configurar los parámetros para la validación del JWT
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidIssuer = builder.Configuration["JwtIssuer"] ?? Environment.GetEnvironmentVariable("JwtIssuer"),
                                ValidateAudience = true,
                                ValidAudience = builder.Configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JwtAudience"),
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = new RsaSecurityKey(rsa)
                            };

                            context.Token = context.Request.Cookies["auth"];
                            return Task.CompletedTask;
                        }
                        catch (Exception e)
                        {
                            // En caso de error, eliminar cookies y redirigir a Login
                            RedirectToLogin(context, collectioncookies);
                            return Task.CompletedTask;
                        }
                    }
                };
            });

            return builder.Services;
        }

        // Método auxiliar para validar claves
        private static bool ValidateKeys(IMemoryCache memoryCache, string userId, IRequestCookieCollection collectioncookies)
        {
            memoryCache.TryGetValue(userId.ToString() + "EncryptedAesKey", out byte[] claveCifrado);
            memoryCache.TryGetValue(userId + "PublicKey", out byte[] publicKeyCifrada);

            return claveCifrado != null && publicKeyCifrada != null;
        }

        // Método auxiliar para redirigir a Login y eliminar cookies
        private static void RedirectToLogin(MessageReceivedContext context, IRequestCookieCollection collectioncookies)
        {
            foreach (var cookie in collectioncookies)
            {
                context.Response.Cookies.Delete(cookie.Key);
            }

            if (context.Request.Path != "/Auth/Login")
            {
                context.Response.Redirect("/Auth/Login");
            }
        }

    }
}