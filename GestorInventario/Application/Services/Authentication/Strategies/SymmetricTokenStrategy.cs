using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class SymmetricTokenStrategy : BaseTokenStrategy
    {
        public SymmetricTokenStrategy(IConfiguration configuration, GestorInventarioContext context)
            : base(configuration, context)
        {
        }

        public override async Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            // Obtener usuario completo de la base de datos
            var usuarioDB = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .FirstOrDefaultAsync(u => u.Id == credencialesUsuario.Id);

            if (usuarioDB == null)
            {
                throw new ArgumentException("El usuario no existe en la base de datos.");
            }

            // Usamos el método de la clase base para crear los claims
            var claims = CrearClaims(credencialesUsuario);

            // Obtener clave secreta
            var clave = Environment.GetEnvironmentVariable("ClaveJWT")
                     ?? _configuration["ClaveJWT"];

            if (string.IsNullOrEmpty(clave))
            {
                throw new InvalidOperationException("La clave JWT no está configurada.");
            }

            var claveKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));

            var signingCredentials = new SigningCredentials(claveKey, SecurityAlgorithms.HmacSha256);

            var securityToken = new JwtSecurityToken(
                issuer: ObtenerIssuer(),
                audience: ObtenerAudience(),
                claims: claims,
                expires: DateTime.Now.AddDays(30),
                signingCredentials: signingCredentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return new LoginResponseDto()
            {
                Id = credencialesUsuario.Id,
                Token = tokenString,
                Rol = usuarioDB.IdRolNavigation?.Nombre ?? "Usuario"
            };
        }
    }
}