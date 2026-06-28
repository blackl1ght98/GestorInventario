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

        public List<Claim> CrearClaims(Usuario usuario) => new()
    {
        new Claim(ClaimTypes.Email, usuario.Email),
        new Claim(ClaimTypes.Role, usuario.IdRolNavigation?.Nombre ?? "Usuario"),
        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString())
    };

        public string ObtenerIssuer() =>
            Environment.GetEnvironmentVariable("JwtIssuer") ?? _configuration["JwtIssuer"];

        public string ObtenerAudience() =>
            _configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JwtAudience");

        public string ObtenerDuracionAccessTokenMinutos() =>
            _configuration["Jwt:AccessTokenMinutes"] ?? "10";

        public string ObtenerDuracionRefreshTokenHoras() =>
            _configuration["Jwt:RefreshTokenHours"] ?? "24";
    }
}
