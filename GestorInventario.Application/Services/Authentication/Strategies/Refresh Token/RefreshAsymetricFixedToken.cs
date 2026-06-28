using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class RefreshAsymetricFixedToken : IRefreshTokenStrategy
    {
        private readonly TokenClaimsBuilder _claimsBuilder;
        private readonly IConfiguration _configuration;

        public RefreshAsymetricFixedToken(
            TokenClaimsBuilder claimsBuilder,
            IConfiguration configuration)
        {
            _claimsBuilder = claimsBuilder;
            _configuration = configuration;
        }

        public Task<string> GenerarTokenRefresco(Usuario usuario)
        {
            var privateKeyXml = Environment.GetEnvironmentVariable("PrivateKey")
                             ?? _configuration["JWT:PrivateKey"];

            if (string.IsNullOrEmpty(privateKeyXml))
                throw new InvalidOperationException("La clave privada no está configurada.");

            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKeyXml);

            var credentials = new SigningCredentials(
                new RsaSecurityKey(rsa.ExportParameters(true)),
                SecurityAlgorithms.RsaSha256);
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
