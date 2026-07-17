using GestorInventario.Domain.Models;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace GestorInventario.Application.Services.Authentication
{
    /// <summary>
    /// Helper para construir claims, issuer, audience y duraciones.
    /// Evita que las estrategias de access y refresh se acoplen entre sí.
    /// </summary>
    public class TokenClaimsBuilder
    {
        private readonly IConfiguration _configuration;

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
        public string ObtenerPublicKeyFixed()=> _configuration["Jwt:PublicKey"] ?? Environment.GetEnvironmentVariable("PUBLIC_KEY");
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
