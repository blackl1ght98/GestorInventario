using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Services.Authentication;
using GestorInventario.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GestorInventario.Infrastructure.Authentication.AccessToken
{
    public class AsymmetricFixedTokenStrategy : BaseTokenStrategy
    {
        public AsymmetricFixedTokenStrategy(IConfiguration configuration, IJwtTokenSettings claimsBuilder)
            : base(configuration, claimsBuilder) { }

        public override Task<LoginResponseDto> GenerateTokenAsync(Usuario usuario)
        {
            var privateKeyXml = Environment.GetEnvironmentVariable("PRIVATE_KEY")
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
            var minutos = _jwtTokenSettings.ObtenerDuracionAccessTokenMinutos();
            var token = new JwtSecurityToken(
                issuer: _jwtTokenSettings.ObtenerIssuer(),
                audience: _jwtTokenSettings.ObtenerAudience(),
                claims: _jwtTokenSettings.CrearClaims(usuario),
                expires: DateTime.UtcNow.AddMinutes(minutos),
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