using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class AsymmetricFixedTokenStrategy: ITokenStrategy
    {
        private readonly GestorInventarioContext _context;
        private readonly IConfiguration _configuration;

        public AsymmetricFixedTokenStrategy(GestorInventarioContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        public async Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            var usuarioDB = await _context.Usuarios
                 .Include(u => u.IdRolNavigation)
                
                 .FirstOrDefaultAsync(u => u.Id == credencialesUsuario.Id);
         
            var claims = new List<Claim>()
                {
                   new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                    new Claim(ClaimTypes.Role, credencialesUsuario.IdRolNavigation.Nombre),
                    new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())

                };
         
            var privateKey = Environment.GetEnvironmentVariable("PrivateKey") ?? _configuration["JWT:PrivateKey"];

            // Convierte la clave privada a formato RSA
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKey);

            // Crea las credenciales de firma con la clave privada
            var signinCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
            var securityToken = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JwtIssuer") ?? _configuration["JwtIssuer"],
                audience: _configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JwtAudience"),
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: signinCredentials);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return new LoginResponseDto()
            {
                Id = credencialesUsuario.Id,
                Token = tokenString,
                Rol = credencialesUsuario.IdRolNavigation.Nombre,
            };
        }
    }
}
