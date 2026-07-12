
using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Auth;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class AsymmetricFixedTokenStrategy : BaseTokenStrategy
    {
        public AsymmetricFixedTokenStrategy(IConfiguration configuration, TokenClaimsBuilder claimsBuilder)
            : base(configuration, claimsBuilder) { }

        public override Task<LoginResponseDto> GenerateTokenAsync(Usuario usuario)
        {
            var privateKeyXml = Environment.GetEnvironmentVariable("PrivateKey")
                             ?? _configuration["JWT:PrivateKey"];

            if (string.IsNullOrEmpty(privateKeyXml))
                throw new InvalidOperationException("La clave privada no está configurada.");

            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKeyXml);

            var credentials = new SigningCredentials(
                new RsaSecurityKey(rsa.ExportParameters(true))
                {
                    KeyId = usuario.Id.ToString()
                },
                SecurityAlgorithms.RsaSha256);
            var minutos = _claimsBuilder.ObtenerDuracionAccessTokenMinutos();
            var token = new JwtSecurityToken(
                issuer: _claimsBuilder.ObtenerIssuer(),
                audience: _claimsBuilder.ObtenerAudience(),
                claims: _claimsBuilder.CrearClaims(usuario),
                expires: DateTime.UtcNow.AddMinutes(int.Parse(minutos)),
                signingCredentials: credentials);

            return Task.FromResult(new LoginResponseDto
            {
                Id = usuario.Id,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Rol = usuario.IdRolNavigation?.Nombre ?? "Usuario"
            });
        }
    }
}