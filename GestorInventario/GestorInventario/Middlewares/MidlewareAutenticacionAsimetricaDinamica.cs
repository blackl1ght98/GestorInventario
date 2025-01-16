using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

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
                    // Recuperar cookies y servicios necesarios
                    var token = context.Request.Cookies["auth"];
                    var refreshToken = context.Request.Cookies["refreshToken"];
                    var httpContextAccessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();
                    var tokenService = context.RequestServices.GetRequiredService<ITokenGenerator>();
                    var userService = context.RequestServices.GetRequiredService<IAdminRepository>();
                    var redis = context.RequestServices.GetService<IDistributedCache>();
                    var memoryCache = context.RequestServices.GetService<IMemoryCache>();
                    var connectionMultiplexer = context.RequestServices.GetService<IConnectionMultiplexer>();
                    bool useRedis = connectionMultiplexer?.IsConnected ?? false;

                    // Validar el token principal
                    if (!string.IsNullOrEmpty(token))
                    {
                        var jwtToken = await ValidateToken(token, builder, tokenService, redis, memoryCache, useRedis);
                        if (jwtToken == null && !string.IsNullOrEmpty(refreshToken))
                        {
                            await HandleExpiredToken(context, refreshToken, builder, tokenService, userService, redis, memoryCache, useRedis);
                        }
                    }
                    else if (!string.IsNullOrEmpty(refreshToken))
                    {
                        await HandleExpiredToken(context, refreshToken, builder, tokenService, userService, redis, memoryCache, useRedis);
                    }
                }
                catch (Exception ex)
                {
                    var logger = log4net.LogManager.GetLogger(typeof(Program));
                    logger.Error("Error en el middleware de autenticación", ex);
                }

                await next.Invoke();
            });

            return app;
        }

        private static async Task<JwtSecurityToken?> ValidateToken(string token, WebApplicationBuilder builder, ITokenGenerator tokenService, IDistributedCache? redis, IMemoryCache? memoryCache, bool useRedis)
        {
            var handler = new JwtSecurityTokenHandler();

            try
            {
                var jwtToken = handler.ReadJwtToken(token);

                if (jwtToken.ValidTo < DateTime.UtcNow)
                    return null;

                string kid = jwtToken.Header["kid"]?.ToString();
                if (string.IsNullOrEmpty(kid))
                    return null;

                var (privateKey, encryptedAesKey, publicKey) = await RetrieveKeys(kid, redis, memoryCache, useRedis);
                if (privateKey == null || string.IsNullOrEmpty(encryptedAesKey) || string.IsNullOrEmpty(publicKey))
                    return null;

                // Descifrar claves y validar el token
                var aesKey = tokenService.Descifrar(Convert.FromBase64String(encryptedAesKey), privateKey.Value);
                var publicKeyBytes = tokenService.Descifrar(Convert.FromBase64String(publicKey), aesKey);

                var rsaParameters = new RSAParameters { Modulus = publicKeyBytes, Exponent = new byte[] { 1, 0, 1 } };
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(RSA.Create(rsaParameters)),
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JwtAudience"]
                };

                handler.ValidateToken(token, validationParameters, out _);
                return jwtToken;
            }
            catch
            {
                return null;
            }
        }


        private static async Task HandleExpiredToken(HttpContext context, string refreshToken,WebApplicationBuilder builder,ITokenGenerator tokenService,IAdminRepository userService,IDistributedCache? redis,
                                                     IMemoryCache? memoryCache,bool useRedis)
        {
            var refreshTokenValid = await ValidateRefreshToken(refreshToken, builder, redis, memoryCache, useRedis);


            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(refreshToken);
            var userId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return;

            var user = await userService.ObtenerPorId(int.Parse(userId));
            if (user == null)
                return;

            var newAccessToken = await tokenService.GenerarTokenAsimetricoDinamico(user);
            var newRefreshToken = await tokenService.GenerarTokenRefresco(user);

            context.Response.Cookies.Append("auth", newAccessToken.Token, new CookieOptions
            {

                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Domain = "localhost",
                Secure = true,
                Expires = DateTime.UtcNow.AddMinutes(10)
            });

            context.Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {

                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Domain = "localhost",
                Secure = true,
                Expires = DateTime.UtcNow.AddDays(7)
            });
        }

        private static async Task<(RSAParameters?, string?, string?)> RetrieveKeys(string kid,IDistributedCache? redis,IMemoryCache? memoryCache,bool useRedis)
        {
            if (useRedis && redis != null)
            {
                var encryptedAesKey = await redis.GetStringAsync($"{kid}EncryptedAesKey");
                var privateKeyJson = await redis.GetStringAsync($"{kid}PrivateKey");
                var publicKey = await redis.GetStringAsync($"{kid}PublicKey");
                var privateKey = JsonConvert.DeserializeObject<RSAParameters>(privateKeyJson ?? string.Empty);
                return (privateKey, encryptedAesKey, publicKey);
            }
            else if (memoryCache != null)
            {
                memoryCache.TryGetValue($"{kid}EncryptedAesKey", out string? encryptedAesKey);
                memoryCache.TryGetValue($"{kid}PrivateKey", out string? privateKeyJson);
                memoryCache.TryGetValue($"{kid}PublicKey", out string? publicKey);
                var privateKey = JsonConvert.DeserializeObject<RSAParameters>(privateKeyJson ?? string.Empty);
                return (privateKey, encryptedAesKey, publicKey);
            }

            return (null, null, null);
        }

        private static async Task<bool> ValidateRefreshToken(string refreshToken,WebApplicationBuilder builder,IDistributedCache? redis,IMemoryCache? memoryCache,bool useRedis)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(refreshToken);
                if (token.ValidTo < DateTime.UtcNow)
                    return false;

                string kid = token.Header["kid"]?.ToString();
                if (string.IsNullOrEmpty(kid))
                    return false;

                var (privateKey, _, publicKey) = await RetrieveKeys(kid, redis, memoryCache, useRedis);
                if (privateKey == null || publicKey == null)
                    return false;

                var rsaSecurityKey = new RsaSecurityKey(new RSAParameters
                {
                    Modulus = Convert.FromBase64String(publicKey),
                    Exponent = new byte[] { 1, 0, 1 }
                });

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = rsaSecurityKey,
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JwtAudience"]
                };

                handler.ValidateToken(refreshToken, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
