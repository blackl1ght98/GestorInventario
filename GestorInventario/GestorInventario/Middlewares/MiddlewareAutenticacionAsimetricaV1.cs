using GestorInventario.Application.Services;
using GestorInventario.Interfaces.Application;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GestorInventario.Middlewares
{
    public static class MiddlewareAutenticacionAsimetricaV1
    {
        public static IApplicationBuilder MiddlewareAutenticacionAsimetrica(this IApplicationBuilder app, WebApplicationBuilder builder)
        {
            app.Use(async (context, next) =>
            {
                var logger = log4net.LogManager.GetLogger(typeof(Program));

                try
                {
                    var collectionCookies = context.Request.Cookies;
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
                        var userId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                       if(userId == null)
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

                        string privateKeyJson;
                        string publicKeyJson;

                        if (useRedis)
                        {
                            privateKeyJson = await redis.GetStringAsync(userId + "PrivateKey");
                            publicKeyJson = await redis.GetStringAsync(userId + "PublicKey");
                        }
                        else
                        {
                            memoryCache.TryGetValue(userId + "PrivateKey", out privateKeyJson);
                            memoryCache.TryGetValue(userId + "PublicKey", out publicKeyJson);
                        }

                     
                        if (privateKeyJson == null || publicKeyJson == null) 
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

                        // Recuperar y configurar las claves RSA
                        var privateKey = JsonConvert.DeserializeObject<RSAParameters>(privateKeyJson);
                        var publicKey = JsonConvert.DeserializeObject<RSAParameters>(publicKeyJson);

                        var principal = handler.ValidateToken(token, new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new RsaSecurityKey(RSA.Create(new RSAParameters
                            {
                                Modulus = publicKey.Modulus,
                                Exponent = publicKey.Exponent
                            })),
                            ValidateIssuer = true,
                            ValidIssuer = builder.Configuration["JwtIssuer"] ?? Environment.GetEnvironmentVariable("JwtIssuer"),
                            ValidateAudience = true,
                            ValidAudience = builder.Configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JwtAudience"),
                        }, out var validatedToken);

                        context.User = principal;
                        token = context.Session.GetString("auth") ?? context.Request.Cookies["auth"];
                        context.Session.SetString("auth", token);
                    }
                }
                catch (Exception ex)
                {
                    var collectioncookies = context.Request.Cookies;

                    logger.Error("Error con las claves durante el proceso de autenticación", ex);
                    foreach (var cookie in collectioncookies)
                    {
                        context.Response.Cookies.Delete(cookie.Key);
                    }
                    if (context.Request.Path != "/Auth/Login")
                    {
                        context.Response.Redirect("/Auth/Login");
                    }
                }

                await next.Invoke();
            });

            return app;
        }

        
    }
}
