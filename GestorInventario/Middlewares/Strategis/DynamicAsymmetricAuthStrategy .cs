using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Common;
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
            var utility = context.RequestServices.GetRequiredService<IUserRepository>();
            var cache = context.RequestServices.GetRequiredService<ICacheService>(); 
            var configuration = context.RequestServices.GetService<IConfiguration>();

            if (!string.IsNullOrEmpty(token))
            {
               
                var (jwtToken, principal) = await ValidateToken(token, configuration, cache);

                if (jwtToken != null && principal != null)
                {
                    context.User = principal;
                }
                else if (!string.IsNullOrEmpty(refreshToken))
                {
                    await HandleExpiredToken(context, refreshToken, configuration, tokenService, utility, cache);
                }
            }
            else if (!string.IsNullOrEmpty(refreshToken))
            {
                await HandleExpiredToken(context, refreshToken, configuration, tokenService, utility, cache);
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
        ICacheService cache) 
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

            // Usamos la abstracción de caché para recuperar la clave pública
            var publicKeyParams = await RetrievePublicKey(kid, cache);
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
                ClockSkew = TimeSpan.FromMinutes(5),
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

    private static async Task<RSAParameters?> RetrievePublicKey(string kid, ICacheService cache)
    {
        string? publicKeyJson = await cache.GetStringAsync($"{kid}PublicKey");

        if (string.IsNullOrEmpty(publicKeyJson))
            return null;

        try
        {
            return JsonConvert.DeserializeObject<RSAParameters>(publicKeyJson);
        }
        catch (Exception ex)
        {
            var logger = log4net.LogManager.GetLogger(typeof(DynamicAsymmetricAuthStrategy));
            logger.Error($"Error deserializando clave pública {kid}", ex);
            return null;
        }
    }


    private static async Task HandleExpiredToken(
     HttpContext context,
     string refreshToken,
     IConfiguration configuration,
     ITokenGenerator tokenService,
     IUserRepository utility,
     ICacheService cache)
    {
        var logger = log4net.LogManager.GetLogger(typeof(DynamicAsymmetricAuthStrategy));

        try
        {
            var refreshTokenValid = await ValidateRefreshToken(refreshToken, configuration, cache);

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

            
            if (!int.TryParse(userId, out var userIdParsed))
            {
                logger.Warn($"userId en refresh token no es numérico: {userId}");
                RedirectToLogin(context, context.Request.Cookies);
                return;
            }

            var user = await utility.ObtenerUsuarioPorId(userIdParsed);
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
        catch (Exception ex)
        {
            logger.Error("Error en HandleExpiredToken", ex);
        }
    }


    private static async Task<bool> ValidateRefreshToken(
        string refreshToken,
        IConfiguration configuration,
        ICacheService cache)
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

            string? publicKeyJson = await cache.GetStringAsync($"{kid}PublicKeyRefresco");
            if (string.IsNullOrEmpty(publicKeyJson))
                return false;

            var publicKeyParams = JsonConvert.DeserializeObject<RSAParameters>(publicKeyJson);
            

        
            var rsaSecurityKey = new RsaSecurityKey(publicKeyParams)
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

            handler.ValidateToken(refreshToken, validationParameters, out _);
            return true;
        }
        catch (Exception ex)
        {

            var logger = log4net.LogManager.GetLogger(typeof(DynamicAsymmetricAuthStrategy));
            logger.Error("Error de validación de RefreshToken", ex);
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