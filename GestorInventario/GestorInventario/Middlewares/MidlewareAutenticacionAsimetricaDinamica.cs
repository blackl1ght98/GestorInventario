using GestorInventario.Application.Services;
using GestorInventario.Interfaces.Application;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GestorInventario.Middlewares
{
    public static class MidlewareAutenticacionAsimetricaDinamica
    {
        public static IApplicationBuilder MiddlewareAutenticacionAsimetricaDinamica(this IApplicationBuilder app, WebApplicationBuilder builder)
        {

            app.Use(async (context, next) =>
            {
                try
                {
                    var cookies = context.Request.Cookies;
                    var httpContextAccessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();
                    var tokenService = context.RequestServices.GetRequiredService<ITokenGenerator>();
                    var redis = context.RequestServices.GetRequiredService<IDistributedCache>();
                    var memoryCache = context.RequestServices.GetRequiredService<IMemoryCache>();
                    var connectionMultiplexer = context.RequestServices.GetService<IConnectionMultiplexer>();
                    bool useRedis = connectionMultiplexer != null && connectionMultiplexer.GetDatabase().Ping().Milliseconds >= 0;

                    var token = context.Request.Cookies["auth"];
                    if (token != null)
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

                        if (userId == null)
                        {
                            RedirectLogin(context, cookies);
                            return;
                        }

                        string encryptedAesKeyBase64;
                        string privateKeyJson;
                        if (useRedis)
                        {
                            encryptedAesKeyBase64 = await redis.GetStringAsync(userId + "EncryptedAesKey");
                            privateKeyJson = await redis.GetStringAsync(userId + "PrivateKey");
                        }
                        else
                        {
                            memoryCache.TryGetValue(userId + "EncryptedAesKey", out encryptedAesKeyBase64);
                            memoryCache.TryGetValue(userId + "PrivateKey", out privateKeyJson);
                        }

                        if (string.IsNullOrEmpty(encryptedAesKeyBase64) || string.IsNullOrEmpty(privateKeyJson))
                        {
                            RedirectLogin(context, cookies);
                            return;
                        }

                        var encryptedAesKey = Convert.FromBase64String(encryptedAesKeyBase64);
                        var privateKey = JsonConvert.DeserializeObject<RSAParameters>(privateKeyJson);

                        if (privateKey.Modulus == null || privateKey.Exponent == null)
                        {
                            RedirectLogin(context, cookies);
                            return;
                        }

                        var aesKey = tokenService.Descifrar(encryptedAesKey, privateKey);

                        string publicKeyCifradaBase64;
                        if (useRedis)
                        {
                            publicKeyCifradaBase64 = await redis.GetStringAsync(userId + "PublicKey");
                        }
                        else
                        {
                            memoryCache.TryGetValue(userId + "PublicKey", out publicKeyCifradaBase64);
                        }

                        if (string.IsNullOrEmpty(publicKeyCifradaBase64))
                        {
                            RedirectLogin(context, cookies);
                            return;
                        }

                        var publicKeyCifrada = Convert.FromBase64String(publicKeyCifradaBase64);
                        var publicKey = tokenService.Descifrar(publicKeyCifrada, aesKey);

                        if (publicKey == null || publicKey.Length == 0)
                        {
                            RedirectLogin(context, cookies);
                            return;
                        }

                        try
                        {
                            var rsaParameters = new RSAParameters
                            {
                                Modulus = publicKey,
                                Exponent = new byte[] { 1, 0, 1 }
                            };

                            var principal = handler.ValidateToken(token, new TokenValidationParameters
                            {
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = new RsaSecurityKey(RSA.Create(rsaParameters)),
                                ValidateIssuer = true,
                                ValidIssuer = builder.Configuration["JwtIssuer"],
                                ValidateAudience = true,
                                ValidAudience = builder.Configuration["JwtAudience"]
                            }, out _);

                            context.User = principal;
                        }
                        catch (Exception ex)
                        {
                            // Manejo de error durante la validación del token
                            RedirectLogin(context, cookies);
                            var logger = log4net.LogManager.GetLogger(typeof(Program));
                            logger.Error("Error validando el token", ex);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    RedirectLogin(context, context.Request.Cookies);
                    var logger = log4net.LogManager.GetLogger(typeof(Program));
                    logger.Error("Error en el middleware", ex);
                }

                await next.Invoke();
            });

            return app;
        }

        private static void RedirectLogin(HttpContext context, IRequestCookieCollection cookies)
        {
            foreach (var cookie in cookies)
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