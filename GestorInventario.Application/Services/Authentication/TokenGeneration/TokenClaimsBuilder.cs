using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.TokenGeneration
{
    /// <summary>
    /// Helper para construir claims, issuer, audience y duraciones.
    /// Evita que las estrategias de access y refresh se acoplen entre sí.
    /// </summary>
    public class TokenClaimsBuilder: ITokenClaimsBuilder
    {
        private readonly IConfiguration _configuration;
        private RSA? _publicKeyFixedCache;
        private readonly object _publicKeyLock = new();
        public TokenClaimsBuilder(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IReadOnlyList<Claim> CrearClaims(Usuario usuario)
        {
            if (usuario.IdRolNavigation is null)
            {
                throw new InvalidOperationException(
                    $"El usuario {usuario.Id} no tiene un rol asignado. No se puede generar token.");
            }

            return new[]
            {
              new Claim(ClaimTypes.Email, usuario.Email),
              new Claim(ClaimTypes.Role, usuario.IdRolNavigation.Nombre),
              new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString())
            };
        }

        public string ObtenerIssuer() =>
            Environment.GetEnvironmentVariable("JWT_ISSUER") ?? _configuration["Jwt:Issuer"];

        public string ObtenerAudience() =>
            _configuration["Jwt:Audience"] ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE");

        public int ObtenerDuracionAccessTokenMinutos() =>
        ResolveInt("Jwt:AccessTokenMinutes", "ACCESS_TOKEN_MINUTES", defaultValue: 10);

        public int ObtenerDuracionRefreshTokenHoras() =>
         ResolveInt("Jwt:RefreshTokenHours", "REFRESH_TOKEN_HOURS", defaultValue: 24);
        public RSA ObtenerPublicKeyFixed()
        {
            if (_publicKeyFixedCache is not null) return _publicKeyFixedCache;

            lock (_publicKeyLock)
            {
                if (_publicKeyFixedCache is not null) return _publicKeyFixedCache;

                var raw = _configuration["Jwt:PublicKey"]
                          ?? Environment.GetEnvironmentVariable("PUBLIC_KEY");

                if (string.IsNullOrWhiteSpace(raw))
                {
                    throw new InvalidOperationException(
                        "La clave pública JWT fija no está configurada. " +
                        "Solo se requiere cuando el modo de autenticación es AsymmetricFixed.");
                }

                try
                {
                    var rsa = RSA.Create();
                    rsa.FromXmlString(raw);
                    _publicKeyFixedCache = rsa;
                    return rsa;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "La clave pública JWT fija no es un XML RSA válido.", ex);
                }
            }
        }
        public string ObtenerClaveJWT()=> Environment.GetEnvironmentVariable("ClaveJWT") ?? _configuration["JWT:ClaveJWT"];
        private int ResolveInt(string configKey, string envKey, int defaultValue)
        {
            var raw = Environment.GetEnvironmentVariable(envKey) ?? _configuration[configKey];
            if (string.IsNullOrWhiteSpace(raw))
            {
                return defaultValue;
            }

            if (!int.TryParse(raw, out var value) || value <= 0)
            {
                throw new InvalidOperationException(
                    $"El valor de {configKey} (o {envKey}) debe ser un entero positivo. Recibido: '{raw}'.");
            }

            return value;
        }
    }
}
