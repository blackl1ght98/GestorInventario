using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class RefreshSymmetricToken : IRefreshTokenStrategy
    {
        private readonly TokenClaimsBuilder _claimsBuilder;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RefreshSymmetricToken> _logger;

        public RefreshSymmetricToken(
            TokenClaimsBuilder claimsBuilder,
            IConfiguration configuration,
            ILogger<RefreshSymmetricToken> logger)
        {
            _claimsBuilder = claimsBuilder;
            _configuration = configuration;
            _logger = logger;
        }

        public Task<string> GenerarTokenRefresco(Usuario usuario)
        {
            var clave = Environment.GetEnvironmentVariable("ClaveJWT")
                     ?? _configuration["ClaveJWT"];

            if (string.IsNullOrEmpty(clave))
            {
                _logger.LogError("La clave JWT no está configurada.");
                throw new InvalidOperationException("La clave JWT no está configurada.");
            }

            if (Encoding.UTF8.GetByteCount(clave) < 32)
            {
                _logger.LogError("La clave JWT es demasiado corta para HMAC-SHA256.");
                throw new InvalidOperationException("La clave JWT debe tener al menos 32 bytes.");
            }

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
                SecurityAlgorithms.HmacSha256);
            var minutos = _claimsBuilder.ObtenerDuracionAccessTokenMinutos();
            var token = new JwtSecurityToken(
                issuer: _claimsBuilder.ObtenerIssuer(),
                audience: _claimsBuilder.ObtenerAudience(),
                claims: _claimsBuilder.CrearClaims(usuario),
                expires: DateTime.UtcNow.AddHours(int.Parse(minutos)),
                signingCredentials: credentials);

            return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
        }
    }
}
