using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class SymmetricTokenStrategy : BaseTokenStrategy
    {
        public SymmetricTokenStrategy(IConfiguration configuration, TokenClaimsBuilder claimsBuilder)
            : base(configuration, claimsBuilder) { }

        public override Task<LoginResponseDto> GenerateTokenAsync(Usuario usuario)
        {
            var clave = Environment.GetEnvironmentVariable("ClaveJWT")
                     ?? _configuration["ClaveJWT"];

            if (string.IsNullOrEmpty(clave))
                throw new InvalidOperationException("La clave JWT no está configurada.");

            if (Encoding.UTF8.GetByteCount(clave) < 32)
                throw new InvalidOperationException("La clave JWT debe tener al menos 32 bytes.");

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _claimsBuilder.ObtenerIssuer(),
                audience: _claimsBuilder.ObtenerAudience(),
                claims: _claimsBuilder.CrearClaims(usuario),
                expires: DateTime.UtcNow.AddMinutes(_claimsBuilder.ObtenerDuracionAccessTokenMinutos()),
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