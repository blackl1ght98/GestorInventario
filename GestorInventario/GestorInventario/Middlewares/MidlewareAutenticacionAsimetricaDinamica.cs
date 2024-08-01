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
                    var collectioncookies = context.Request.Cookies;
                    var httpContextAccessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();
                    var tokenservice = context.RequestServices.GetRequiredService<ITokenGenerator>();
                    var redis = context.RequestServices.GetRequiredService<IDistributedCache>();
                    var memoryCache = context.RequestServices.GetRequiredService<IMemoryCache>();

                    var connectionMultiplexer = context.RequestServices.GetService<IConnectionMultiplexer>();
                    bool useRedis = connectionMultiplexer != null && connectionMultiplexer.GetDatabase().Ping().Milliseconds >= 0;

                    var token = context.Request.Cookies["auth"];
                    if (token != null)
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var userId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

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
                            foreach (var cookie in collectioncookies)
                            {
                                context.Response.Cookies.Delete(cookie.Key);
                            }
                            if (context.Request.Path != "/Auth/Login")
                            {
                                context.Response.Redirect("/Auth/Login");
                            }
                        }
                        else
                        {
                            var encryptedAesKey = Convert.FromBase64String(encryptedAesKeyBase64);
                            var privateKey = JsonConvert.DeserializeObject<RSAParameters>(privateKeyJson);

                            var aesKey = tokenservice.Descifrar(encryptedAesKey, privateKey);

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
                                foreach (var cookie in collectioncookies)
                                {
                                    context.Response.Cookies.Delete(cookie.Key);
                                }
                                if (context.Request.Path != "/Auth/Login")
                                {
                                    context.Response.Redirect("/Auth/Login");
                                }
                            }
                            else
                            {
                                var publicKeyCifrada = Convert.FromBase64String(publicKeyCifradaBase64);
                                var publicKey = tokenservice.Descifrar(publicKeyCifrada, aesKey);

                                var principal = handler.ValidateToken(token, new TokenValidationParameters
                                {
                                    ValidateIssuerSigningKey = true,
                                    IssuerSigningKey = new RsaSecurityKey(RSA.Create(new RSAParameters
                                    {
                                        Modulus = publicKey,
                                        Exponent = new byte[] { 1, 0, 1 }
                                    })),
                                    ValidateIssuer = true,
                                    ValidIssuer = Environment.GetEnvironmentVariable("JwtIssuer") ?? builder.Configuration["JwtIssuer"],
                                    ValidateAudience = true,
                                    ValidAudience = Environment.GetEnvironmentVariable("JwtAudience") ?? builder.Configuration["JwtAudience"],
                                }, out var validatedToken);

                                context.User = principal;
                                token = context.Session.GetString("auth") ?? context.Request.Cookies["auth"];
                                context.Session.SetString("auth", token);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var collectioncookies = context.Request.Cookies;
                    foreach (var cookie in collectioncookies)
                    {
                        context.Response.Cookies.Delete(cookie.Key);
                    }
                    if (context.Request.Path != "/Auth/Login")
                    {
                        context.Response.Redirect("/Auth/Login");
                    }
                    var logger = log4net.LogManager.GetLogger(typeof(Program));
                    logger.Error("Error con las claves", ex);
                }

                await next.Invoke();
            });



            return app;
        }
    }
}
