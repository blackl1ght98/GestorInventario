using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Interfaces.Caching;
using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Infrastructure.Interfaces.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.Authentication.Middleware;

/// <summary>
/// Estrategia JWT asimétrica con claves RSA dinámicas por usuario.
/// Las claves viven en caché (Redis/Memory) indexadas por 'kid' del header del token.
/// </summary>
public class DynamicAsymmetricAuthStrategy : AuthenticationStrategyBase
{
    
    private readonly IHybridCacheService _cache;
    private readonly ILogger<DynamicAsymmetricAuthStrategy> _logger;

    public DynamicAsymmetricAuthStrategy(
     
        ITokenService tokenService,
        IUserRepository userRepository,
        IRefreshTokenService refreshTokenStrategy,
        IJwtTokenSettings jwtTokenSettings,
        IHybridCacheService cache,
        ILogger<DynamicAsymmetricAuthStrategy> logger)
    :base(tokenService,userRepository,refreshTokenStrategy,jwtTokenSettings)
    {

       
        _cache = cache;
        _logger = logger;
       
    }
    protected override ILogger GetLogger() => _logger;

    protected override async Task<ClaimsPrincipal?> ValidateAccessTokenAsync(string token)
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
                ValidIssuer = _jwtTokenSettings.ObtenerIssuer(),
                ValidateAudience = true,
                ValidAudience = _jwtTokenSettings.ObtenerAudience(),
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
    protected override async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
{
    string? kid = null;
    string? subject = null;

    try
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(refreshToken);

        kid = jwtToken.Header.Kid;
        subject = jwtToken.Claims.FirstOrDefault(c =>
            c.Type == JwtRegisteredClaimNames.Sub ||
            c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (jwtToken.ValidTo < DateTime.UtcNow)
        {
            _logger.LogWarning("Refresh token expirado para kid: {Kid}, sub: {Sub}", kid ?? "N/A", subject ?? "N/A");
            return false;
        }

        if (string.IsNullOrEmpty(kid))
        {
            _logger.LogWarning("Refresh token sin 'kid' para sub: {Sub}", subject ?? "N/A");
            return false;
        }

        var publicKeyParams = await RetrievePublicKeyAsync($"{kid}PublicKeyRefresco");
        if (publicKeyParams is null)
        {
            _logger.LogWarning("Clave pública de refresco no encontrada en caché para kid: {Kid}, sub: {Sub}", kid, subject ?? "N/A");
            return false;
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(publicKeyParams.Value) { KeyId = kid },
            ValidateIssuer = true,
            ValidIssuer = _jwtTokenSettings.ObtenerIssuer(),
            ValidateAudience = true,
            ValidAudience = _jwtTokenSettings.ObtenerAudience(),
            ValidateLifetime = true
        };

        handler.ValidateToken(refreshToken, validationParameters, out _);
        return true;
    }
    catch (SecurityTokenInvalidSignatureException)
    {
        _logger.LogWarning("Refresh token rechazado por firma inválida. kid: {Kid}, sub: {Sub}", kid ?? "N/A", subject ?? "N/A");
        return false;
    }
    catch (SecurityTokenExpiredException)
    {
        _logger.LogWarning("Refresh token expirado durante validación. kid: {Kid}, sub: {Sub}", kid ?? "N/A", subject ?? "N/A");
        return false;
    }
    catch (SecurityTokenException ex)
    {
        _logger.LogWarning("Refresh token inválido ({Reason}). kid: {Kid}, sub: {Sub}", ex.GetType().Name, kid ?? "N/A", subject ?? "N/A");
        return false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error inesperado validando refresh token. kid: {Kid}, sub: {Sub}", kid ?? "N/A", subject ?? "N/A");
        return false;
    }
}
   
    
}