
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies.Middleware;

/// <summary>
/// Estrategia JWT asimétrica con claves RSA dinámicas por usuario.
/// Las claves viven en caché (Redis/Memory) indexadas por 'kid' del header del token.
/// </summary>
public class DynamicAsymmetricAuthStrategy : IAuthenticationMiddlewareStrategy
{
    private readonly IConfiguration _options;
    private readonly ITokenGenerator _tokenService;
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cache;
    private readonly ILogger<DynamicAsymmetricAuthStrategy> _logger;

    public DynamicAsymmetricAuthStrategy(
        IConfiguration options,
        ITokenGenerator tokenService,
        IUserRepository userRepository,
        ICacheService cache,
        ILogger<DynamicAsymmetricAuthStrategy> logger)
    {
        _options = options;
        _tokenService = tokenService;
        _userRepository = userRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task ProcessAuthentication(HttpContext context, Func<Task> next)
    {
        try
        {
            var token = context.Request.Cookies["auth"];
            var refreshToken = context.Request.Cookies["refreshToken"];

            if (!string.IsNullOrEmpty(token))
            {
                var principal = await ValidateTokenAsync(token);
                if (principal is not null)
                {
                    context.User = principal;
                }
                else if (!string.IsNullOrEmpty(refreshToken))
                {
                    await HandleExpiredTokenAsync(context, refreshToken);
                }
            }
            else if (!string.IsNullOrEmpty(refreshToken))
            {
                await HandleExpiredTokenAsync(context, refreshToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en el middleware de autenticación dinámica");
        }

        await next();
    }

    private async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("Token expirado");
                return null;
            }

            var kid = jwtToken.Header["kid"]?.ToString();
            if (string.IsNullOrEmpty(kid))
            {
                _logger.LogWarning("Token sin 'kid'");
                return null;
            }

            var publicKeyParams = await RetrievePublicKeyAsync($"{kid}PublicKey");
            if (publicKeyParams is null)
            {
                _logger.LogWarning("Clave pública no encontrada en caché para kid: {Kid}", kid);
                return null;
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(publicKeyParams.Value) { KeyId = kid },
                ValidateIssuer = true,
                ClockSkew = TimeSpan.FromMinutes(5),
                ValidIssuer = _options["JwtIssuer"],
                ValidateAudience = true,
                ValidAudience = _options["JwtAudience"],
                ValidateLifetime = true
            };

            return handler.ValidateToken(token, validationParameters, out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar token");
            return null;
        }
    }

    private async Task<RSAParameters?> RetrievePublicKeyAsync(string cacheKey)
    {
        var publicKeyJson = await _cache.GetStringAsync(cacheKey);
        if (string.IsNullOrEmpty(publicKeyJson)) return null;

        try
        {
            return JsonConvert.DeserializeObject<RSAParameters>(publicKeyJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializando clave pública {Key}", cacheKey);
            return null;
        }
    }

    private async Task HandleExpiredTokenAsync(HttpContext context, string refreshToken)
    {
        try
        {
            if (!await ValidateRefreshTokenAsync(refreshToken))
            {
                RedirectToLogin(context);
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(refreshToken);
            var userIdClaim = token.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                RedirectToLogin(context);
                return;
            }

            var user = await _userRepository.ObtenerUsuarioPorId(userId);
            if (user is null)
            {
                RedirectToLogin(context);
                return;
            }

            var newAccessToken = await _tokenService.GenerateTokenAsync(user);

            context.Response.Cookies.Append("auth", newAccessToken.Token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = true,
                Expires = DateTime.UtcNow.AddMinutes(10)
            });

            _logger.LogInformation("Access token regenerado para usuario {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al refrescar token");
        }
    }

    private async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(refreshToken);

            if (jwtToken.ValidTo < DateTime.UtcNow) return false;

            var kid = jwtToken.Header.Kid;
            if (string.IsNullOrEmpty(kid)) return false;

            var publicKeyJson = await _cache.GetStringAsync($"{kid}PublicKeyRefresco");
            if (string.IsNullOrEmpty(publicKeyJson)) return false;

            var publicKeyParams = JsonConvert.DeserializeObject<RSAParameters>(publicKeyJson);
           

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(publicKeyParams) { KeyId = kid },
                ValidateIssuer = true,
                ValidIssuer = _options["JwtIssuer"],
                ValidateAudience = true,
                ValidAudience = _options["JwtAudience"],
                ValidateLifetime = true
            };

            handler.ValidateToken(refreshToken, validationParameters, out _);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validando refresh token");
            return false;
        }
    }

    private void RedirectToLogin(HttpContext context)
    {
        foreach (var cookie in context.Request.Cookies)
        {
            context.Response.Cookies.Delete(cookie.Key);
        }

        if (!context.Request.Path.StartsWithSegments("/Auth/Login"))
        {
            context.Response.Redirect("/Auth/Login");
        }
    }
}