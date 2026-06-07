using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

/// <summary>
/// Estrategia de autenticación JWT asimétrica con claves dinámicas por usuario.
/// 
/// Modelo de seguridad:
/// - Cada usuario tiene su propio par de claves RSA generado en el login.
/// - La clave privada RSA se cifra con una clave maestra AES estática antes de guardarse.
/// - La clave AES se cifra con la clave pública RSA antes de guardarse.
/// - Ambas se almacenan en Redis (o MemoryCache como fallback) con el userId como clave.
/// 
/// Esto significa que si alguien roba la caché, no puede descifrar nada sin la clave maestra.
/// 
/// Flujo de autenticación:
/// 1. Cookie "auth" presente y válida → establece contexto de usuario directamente.
/// 2. Cookie "auth" expirada + "refreshToken" válido → regenera access token.
/// 3. Ambos fallan o ausentes → limpia cookies y redirige a login.
/// </summary>
public class DynamicAsymmetricAuthStrategy : IAuthenticationMiddlewareStrategy
{
    public async Task ProcessAuthentication(HttpContext context, Func<Task> next)
    {
        try
        {
            var token = context.Request.Cookies["auth"];
            var refreshToken = context.Request.Cookies["refreshToken"];

            var tokenService = context.RequestServices.GetRequiredService<ITokenGenerator>();
            var encryptionService = context.RequestServices.GetRequiredService<IEncryptionService>();
            var utility = context.RequestServices.GetRequiredService<IUserRepository>();
            var refreshTokenMethod = context.RequestServices.GetRequiredService<IRefreshTokenMethod>();
            var redis = context.RequestServices.GetService<IDistributedCache>();
            var memoryCache = context.RequestServices.GetService<IMemoryCache>();
            var connectionMultiplexer = context.RequestServices.GetService<IConnectionMultiplexer>();
            var configuration = context.RequestServices.GetService<IConfiguration>();
            bool useRedis = connectionMultiplexer?.IsConnected ?? false;

            if (!string.IsNullOrEmpty(token))
            {
                var (jwtToken, principal) = await ValidateToken(
                    token, configuration, tokenService, redis, memoryCache, encryptionService, useRedis);

                if (jwtToken != null && principal != null)
                {
                    context.User = principal;
                }
                else if (!string.IsNullOrEmpty(refreshToken))
                {
                    await HandleExpiredToken(context, refreshToken, configuration, tokenService,
                        utility, redis, memoryCache, refreshTokenMethod, useRedis);
                }
            }
            else if (!string.IsNullOrEmpty(refreshToken))
            {
                await HandleExpiredToken(context, refreshToken, configuration, tokenService,
                    utility, redis, memoryCache, refreshTokenMethod, useRedis);
            }
        }
        catch (Exception ex)
        {
            var logger = log4net.LogManager.GetLogger(typeof(DynamicAsymmetricAuthStrategy));
            logger.Error("Error en el middleware de autenticación", ex);
        }

        await next();
    }

    private static async Task<(JwtSecurityToken?, ClaimsPrincipal?)> ValidateToken(
        string token,
        IConfiguration configuration,
        ITokenGenerator tokenService,
        IDistributedCache? redis,
        IMemoryCache? memoryCache,
        IEncryptionService encryptionService,
        bool useRedis)
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

            // CORRECCIÓN: recuperamos la CLAVE PÚBLICA para validar, no la privada
            var publicKeyParams = await RetrievePublicKey(kid, redis, memoryCache, useRedis);
            if (publicKeyParams == null)
            {
                logger.Warn($"Clave pública no encontrada en caché para kid: {kid}. Posible reinicio de servidor.");
                return (null, null);
            }

            var rsaSecurityKey = new RsaSecurityKey(publicKeyParams.Value)
            {
                KeyId = kid
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

            var principal = handler.ValidateToken(token, validationParameters, out _);
            return (jwtToken, principal);
        }
        catch (Exception ex)
        {
            logger.Error($"Error al validar el token: {ex.Message}", ex);
            return (null, null);
        }
    }

    /// <summary>
    /// Recupera la clave pública RSA del caché para validar la firma del JWT.
    /// La pública se guarda en texto plano porque no es un secreto.
    /// </summary>
    private static async Task<RSAParameters?> RetrievePublicKey(
        string kid,
        IDistributedCache? redis,
        IMemoryCache? memoryCache,
        bool useRedis)
    {
        string? publicKeyJson = null;

        if (useRedis && redis != null)
            publicKeyJson = await redis.GetStringAsync($"{kid}PublicKey");
        else if (memoryCache != null)
            memoryCache.TryGetValue($"{kid}PublicKey", out publicKeyJson);

        if (string.IsNullOrEmpty(publicKeyJson))
            return null;

        try
        {
            return JsonConvert.DeserializeObject<RSAParameters>(publicKeyJson);
        }
        catch
        {
            return null;
        }
    }
    private static async Task HandleExpiredToken(
        HttpContext context,
        string refreshToken,
        IConfiguration configuration,
        ITokenGenerator tokenService,
        IUserRepository utility,
        IDistributedCache? redis,
        IMemoryCache? memoryCache,
        IRefreshTokenMethod refreshTokenMethod,
        bool useRedis)
    {
        var logger = log4net.LogManager.GetLogger(typeof(DynamicAsymmetricAuthStrategy));
        var refreshTokenValid = await ValidateRefreshToken(
            refreshToken, configuration, redis, memoryCache, useRedis);

        if (!refreshTokenValid)
        {
            logger.Warn("Refresh token no válido o expirado. Redirigiendo a login.");
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

        var user = await utility.ObtenerUsuarioPorId(int.Parse(userId));
        if (user == null)
        {
            logger.Warn($"Usuario con ID {userId} no encontrado en BD.");
            RedirectToLogin(context, context.Request.Cookies);
            return;
        }

        var newAccessToken = await tokenService.GenerateTokenAsync(user);

        context.Response.Cookies.Append("auth", newAccessToken.Token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.None,
            Secure = true,
            Expires = DateTime.UtcNow.AddMinutes(10)
        });

        logger.Info($"Access token regenerado para el usuario {userId}.");
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

            string? publicKeyJson = null;

            if (useRedis && redis != null)
                publicKeyJson = await redis.GetStringAsync($"{kid}PublicKeyRefresco");
            else if (memoryCache != null)
                memoryCache.TryGetValue($"{kid}PublicKeyRefresco", out publicKeyJson);

            if (string.IsNullOrEmpty(publicKeyJson))
                return false;

            var publicKeyParams = JsonConvert.DeserializeObject<RSAParameters>(publicKeyJson);
            using var rsa = RSA.Create();
            rsa.ImportParameters(publicKeyParams);

            var rsaSecurityKey = new RsaSecurityKey(rsa) { KeyId = kid };
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
            context.Response.Cookies.Delete(cookie.Key);

        if (context.Request.Path != "/Auth/Login")
            context.Response.Redirect("/Auth/Login");
    }
}