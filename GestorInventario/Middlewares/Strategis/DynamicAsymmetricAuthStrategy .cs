using GestorInventario.Application.Services.Authentication;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GestorInventario.Middlewares.Strategis
{
    // Capa 4: Implementación - Lógica específica
    public class DynamicAsymmetricAuthStrategy : IAuthProcessingStrategy
    {
        public async Task ProcessAuthentication(HttpContext context, Func<Task> next)
        {
            try
            {
                // Recuperar cookies y servicios necesarios
                var token = context.Request.Cookies["auth"];
                var refreshToken = context.Request.Cookies["refreshToken"];
                var httpContextAccessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();
                var tokenService = context.RequestServices.GetRequiredService<ITokenGenerator>();
                var encryptionService = context.RequestServices.GetRequiredService<IEncryptionService>();
                var userService = context.RequestServices.GetRequiredService<IAdminRepository>();
                var refreshTokenMethod = context.RequestServices.GetRequiredService<IRefreshTokenMethod>();
                var redis = context.RequestServices.GetService<IDistributedCache>();
                var memoryCache = context.RequestServices.GetService<IMemoryCache>();
                var connectionMultiplexer = context.RequestServices.GetService<IConnectionMultiplexer>();
                var configuration = context.RequestServices.GetService<IConfiguration>();
                bool useRedis = connectionMultiplexer?.IsConnected ?? false;

                // Validar el token principal
                if (!string.IsNullOrEmpty(token))
                {
                    var (jwtToken, principal) = await ValidateToken(token, configuration, tokenService, redis, memoryCache, encryptionService, useRedis);
                    if (jwtToken != null && principal != null)
                    {
                        // Establecer el ClaimsPrincipal en HttpContext.User
                        context.User = principal;
                        var logger = log4net.LogManager.GetLogger(typeof(DynamicAsymmetricAuthStrategy));
                        logger.Info($"Claims establecidos en HttpContext.User: {string.Join(", ", principal.Claims.Select(c => $"{c.Type}: {c.Value}"))}");
                    }
                    else if (!string.IsNullOrEmpty(refreshToken))
                    {
                        await HandleExpiredToken(context, refreshToken, configuration, tokenService, userService, redis, memoryCache, refreshTokenMethod, useRedis);
                    }
                }
                else if (!string.IsNullOrEmpty(refreshToken))
                {
                    await HandleExpiredToken(context, refreshToken, configuration, tokenService, userService, redis, memoryCache, refreshTokenMethod, useRedis);
                }
            }
            catch (Exception ex)
            {
                var logger = log4net.LogManager.GetLogger(typeof(DynamicAsymmetricAuthStrategy));
                logger.Error("Error en el middleware de autenticación", ex);
            }

            await next();
        }

        private static async Task<(JwtSecurityToken?, ClaimsPrincipal?)> ValidateToken(string token, IConfiguration configuration, ITokenGenerator tokenService, IDistributedCache? redis, IMemoryCache? memoryCache, IEncryptionService encryptionService, bool useRedis)
        {
            var handler = new JwtSecurityTokenHandler();
            var logger = log4net.LogManager.GetLogger(typeof(DynamicAsymmetricAuthStrategy));

            try
            {
                var jwtToken = handler.ReadJwtToken(token);

                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    logger.Warn("Token ha expirado.");
                    return (null, null);
                }

                string kid = jwtToken.Header["kid"]?.ToString();
                if (string.IsNullOrEmpty(kid))
                {
                    logger.Warn("El token no contiene un 'kid'.");
                    return (null, null);
                }

                var (privateKey, encryptedAesKey, publicKey) = await RetrieveKeys(kid, redis, memoryCache, useRedis);
                if (privateKey == null || string.IsNullOrEmpty(encryptedAesKey) || string.IsNullOrEmpty(publicKey))
                {
                    logger.Warn($"No se pudieron recuperar las claves para el usuario con kid: {kid}");
                    return (null, null);
                }

                // Descifrar claves y validar el token
                var aesKey = encryptionService.Descifrar(Convert.FromBase64String(encryptedAesKey), privateKey.Value);
                var publicKeyBytes = encryptionService.Descifrar(Convert.FromBase64String(publicKey), aesKey);

                var rsaParameters = new RSAParameters { Modulus = publicKeyBytes, Exponent = new byte[] { 1, 0, 1 } };
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(RSA.Create(rsaParameters)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtAudience"],
                    ValidateLifetime = true
                };

                var principal = handler.ValidateToken(token, validationParameters, out _);

                var tokenPayload = jwtToken.Claims.Select(c => $"{c.Type}: {c.Value}");
                logger.Info($"Claims validados en el token: {string.Join(", ", tokenPayload)}");

                return (jwtToken, principal);
            }
            catch (Exception ex)
            {
                logger.Error($"Error al validar el token: {ex.Message}", ex);
                return (null, null);
            }
        }

        private static async Task HandleExpiredToken(HttpContext context, string refreshToken, IConfiguration configuration, ITokenGenerator tokenService, IAdminRepository userService, IDistributedCache? redis,
                                                     IMemoryCache? memoryCache, IRefreshTokenMethod refreshTokenMethod, bool useRedis)
        {
            var refreshTokenValid = await ValidateRefreshToken(refreshToken, configuration, redis, memoryCache, useRedis);
            var logger = log4net.LogManager.GetLogger(typeof(DynamicAsymmetricAuthStrategy));

            if (!refreshTokenValid)
            {
                logger.Warn("Refresh token no válido o expirado.");
                RedirectToLogin(context, context.Request.Cookies);
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(refreshToken);
            var userId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                logger.Warn("No se encontró userId en el refresh token.");
                RedirectToLogin(context, context.Request.Cookies);
                return;
            }

            var (user, mensaje) = await userService.ObtenerPorId(int.Parse(userId));
            if (user == null)
            {
                logger.Warn($"Usuario con ID {userId} no encontrado.");
                RedirectToLogin(context, context.Request.Cookies);
                return;
            }

            var newAccessToken = await tokenService.GenerateTokenAsync(user);
            var newRefreshToken = await refreshTokenMethod.GenerarTokenRefresco(user);

            context.Response.Cookies.Append("auth", newAccessToken.Token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Domain = "localhost",
                Secure = true,
                Expires = DateTime.UtcNow.AddMinutes(10)
            });

            context.Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Domain = "localhost",
                Secure = true,
                Expires = DateTime.UtcNow.AddHours(24)
            });

            logger.Info($"Nuevos tokens generados para el usuario {userId}.");
        }

        public static async Task<(RSAParameters?, string?, string?)> RetrieveKeys(string kid, IDistributedCache? redis, IMemoryCache? memoryCache, bool useRedis)
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

        private static async Task<bool> ValidateRefreshToken(
      string refreshToken,
      IConfiguration configuration,
      IDistributedCache? redis,
      IMemoryCache? memoryCache,
      bool useRedis)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(refreshToken);

                if (jwtToken.ValidTo < DateTime.UtcNow)
                    return false;

                string kid = jwtToken.Header.Kid;
                if (string.IsNullOrEmpty(kid))
                    return false;

                // 🔑 recuperar PUBLIC KEY JSON
                string? publicKeyJson = null;

                if (useRedis && redis != null)
                    publicKeyJson = await redis.GetStringAsync($"{kid}PublicKeyRefresco");
                else if (memoryCache != null)
                    memoryCache.TryGetValue($"{kid}PublicKeyRefresco", out publicKeyJson);

                if (string.IsNullOrEmpty(publicKeyJson))
                    return false;

                // 🔓 DESERIALIZAR (ESTO ES LA CLAVE)
                var publicKeyParams =
                    JsonConvert.DeserializeObject<RSAParameters>(publicKeyJson);

                var rsa = RSA.Create();
                rsa.ImportParameters(publicKeyParams);

                var rsaSecurityKey = new RsaSecurityKey(rsa)
                {
                    KeyId = kid // 🔥 OBLIGATORIO
                };

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = rsaSecurityKey,

                    ValidateIssuer = true,
                    ValidIssuer = configuration["JwtIssuer"],

                    ValidateAudience = true,
                    ValidAudience = configuration["JwtAudience"],

                    ValidateLifetime = true
                };

                handler.ValidateToken(refreshToken, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }


        private static void RedirectToLogin(HttpContext context, IRequestCookieCollection collectionCookies)
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