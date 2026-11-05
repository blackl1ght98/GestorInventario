using System.IdentityModel.Tokens.Jwt;
using System.Text;
using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Services.Authentication;
using GestorInventario.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GestorInventario.Infrastructure.Authentication.AccessToken
{
    public class SymmetricTokenStrategy : BaseTokenStrategy
    {
        public SymmetricTokenStrategy(IConfiguration configuration, IJwtTokenSettings claimsBuilder)
            : base(configuration, claimsBuilder) { }

        public override Task<LoginResponseDto> GenerateTokenAsync(Usuario usuario)
        {
            var clave = _jwtTokenSettings.ObtenerClaveJWT();

            if (string.IsNullOrEmpty(clave))
                throw new InvalidOperationException("La clave JWT no está configurada.");

            if (Encoding.UTF8.GetByteCount(clave) < 32)
                throw new InvalidOperationException("La clave JWT debe tener al menos 32 bytes.");

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
                SecurityAlgorithms.HmacSha256);
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