using GestorInventario.Application.Services;
using GestorInventario.Interfaces.Application;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GestorInventario.Configuracion.Strategies
{
    public class AsymmetricDynamicAuthenticationStrategy : IAuthenticationStrategy
    {
        public IServiceCollection ConfigureAuthentication(WebApplicationBuilder builder, IConfiguration configuration)
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
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax; 
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; 
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
                        var collectionCookies = context.Request.Cookies;
                        var httpContextAccessor = context.HttpContext.RequestServices.GetRequiredService<IHttpContextAccessor>();
                        var tokenService = context.HttpContext.RequestServices.GetRequiredService<ITokenGenerator>();
                        var encryptionService = context.HttpContext.RequestServices.GetRequiredService<IEncryptionService>();
                        var memoryCache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

                        try
                        {
                            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                            if (string.IsNullOrEmpty(userId) || !ValidateKeys(memoryCache, userId, collectionCookies))
                            {
                                RedirectToLogin(context, collectionCookies);
                                return Task.CompletedTask;
                            }

                            memoryCache.TryGetValue(userId + "PublicKey", out byte[] publicKeyCifrada);
                            memoryCache.TryGetValue(userId + "EncryptedAesKey", out byte[] claveCifrado);

                            if (claveCifrado == null || publicKeyCifrada == null)
                            {
                                RedirectToLogin(context, collectionCookies);
                                return Task.CompletedTask;
                            }

                            var publicKey = Encoding.UTF8.GetString(encryptionService.Descifrar(publicKeyCifrada, claveCifrado));
                            var rsa = new RSACryptoServiceProvider();
                            rsa.FromXmlString(publicKey);

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

                            context.Token = context.Request.Cookies["auth"];
                            return Task.CompletedTask;
                        }
                        catch (Exception)
                        {
                            RedirectToLogin(context, collectionCookies);
                            return Task.CompletedTask;
                        }
                    }
                };
            });

            return builder.Services;
        }

        private static bool ValidateKeys(IMemoryCache memoryCache, string userId, IRequestCookieCollection collectionCookies)
        {
            memoryCache.TryGetValue(userId + "EncryptedAesKey", out byte[] claveCifrado);
            memoryCache.TryGetValue(userId + "PublicKey", out byte[] publicKeyCifrada);
            return claveCifrado != null && publicKeyCifrada != null;
        }

        private static void RedirectToLogin(MessageReceivedContext context, IRequestCookieCollection collectionCookies)
        {
            foreach (var cookie in collectionCookies)
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