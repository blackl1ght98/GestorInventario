using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class SymmetricTokenStrategy : ITokenStrategy
    {
        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;
        public SymmetricTokenStrategy(IConfiguration configuration, GestorInventarioContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task<DTOLoginResponse> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);
            var claims = new List<Claim>()
            {
                 new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                 new Claim(ClaimTypes.Role, credencialesUsuario.IdRolNavigation.Nombre),
                 new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())


            };
            var clave = Environment.GetEnvironmentVariable("ClaveJWT") ?? _configuration["ClaveJWT"];
            var claveKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));
            var signinCredentials = new SigningCredentials(claveKey, SecurityAlgorithms.HmacSha256);
            var securityToken = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JwtIssuer") ?? _configuration["JwtIssuer"],
                audience: Environment.GetEnvironmentVariable("JwtAudience") ?? _configuration["JwtAudience"],
                claims: claims,
                expires: DateTime.Now.AddDays(30),
                signingCredentials: signinCredentials);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);
            return new DTOLoginResponse()
            {
                Id = credencialesUsuario.Id,
                Token = tokenString,
                Rol = credencialesUsuario.IdRolNavigation.Nombre,
            };
        }
    }
}
