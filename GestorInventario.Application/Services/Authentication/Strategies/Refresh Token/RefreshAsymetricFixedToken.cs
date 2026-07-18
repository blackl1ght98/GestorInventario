using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Authentication;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class RefreshAsymetricFixedToken : IRefreshTokenStrategy
    {
        private readonly ITokenClaimsBuilder _claimsBuilder;
        private readonly IConfiguration _configuration;

        public RefreshAsymetricFixedToken(
            ITokenClaimsBuilder claimsBuilder,
            IConfiguration configuration)
        {
            _claimsBuilder = claimsBuilder;
            _configuration = configuration;
        }

        public Task<string> GenerarTokenRefresco(Usuario usuario)
        {
            var privateKeyXml = Environment.GetEnvironmentVariable("PRIVATE_KEY")
                             ?? _configuration["JWT:PrivateKey"];

            if (string.IsNullOrEmpty(privateKeyXml))
                throw new InvalidOperationException("La clave privada no está configurada.");

            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKeyXml);

            var credentials = new SigningCredentials(
                new RsaSecurityKey(rsa.ExportParameters(true)),
                SecurityAlgorithms.RsaSha256);
            var horas = _claimsBuilder.ObtenerDuracionRefreshTokenHoras();
            var token = new JwtSecurityToken(
                issuer: _claimsBuilder.ObtenerIssuer(),
                audience: _claimsBuilder.ObtenerAudience(),
                claims: _claimsBuilder.CrearClaims(usuario),
                expires: DateTime.UtcNow.AddHours(horas),
                signingCredentials: credentials);

            return Task.FromResult(new JwtSecurityTokenHandler().WriteToken(token));
        }
    }
}
