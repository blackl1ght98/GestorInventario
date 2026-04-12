using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class AsymmetricFixedTokenStrategy : BaseTokenStrategy
    {
        public AsymmetricFixedTokenStrategy(
            GestorInventarioContext context,
            IConfiguration configuration)
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

            // Usamos los métodos de la clase base (evitamos duplicar código)
            var claims = CrearClaims(credencialesUsuario);

            // Obtener clave privada fija
            var privateKeyXml = Environment.GetEnvironmentVariable("PrivateKey")
                             ?? _configuration["JWT:PrivateKey"];

            if (string.IsNullOrEmpty(privateKeyXml))
            {
                throw new InvalidOperationException("La clave privada no está configurada.");
            }

            // Cargar clave privada en RSA
            using var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKeyXml);

            // Crear la clave de seguridad con la clave PRIVADA completa
            var rsaSecurityKey = new RsaSecurityKey(rsa.ExportParameters(true))
            {
                KeyId = credencialesUsuario.Id.ToString()
            };

            var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

            // Crear el token
            var securityToken = new JwtSecurityToken(
                issuer: ObtenerIssuer(),
                audience: ObtenerAudience(),
                claims: claims,
                expires: DateTime.Now.AddDays(1),
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