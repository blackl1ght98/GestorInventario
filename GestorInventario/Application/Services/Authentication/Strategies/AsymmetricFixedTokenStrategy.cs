using GestorInventario.Application.DTOs;
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
        public async Task<DTOLoginResponse> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);
            var permisos = usuarioDB.IdRolNavigation.RolePermisos?.Select(rp => rp.Permiso?.Nombre) ?? Enumerable.Empty<string>();
            var permisosList = permisos.ToList();
            var claims = new List<Claim>()
                {
                   new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                    new Claim(ClaimTypes.Role, credencialesUsuario.IdRolNavigation.Nombre),
                    new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())

                };
            foreach (var permiso in permisosList)
            {
                if (!string.IsNullOrEmpty(permiso))
                {
                    claims.Add(new Claim("permiso", permiso, ClaimValueTypes.String, issuer: "GestorInvetarioEmisor"));
                    //_logger.LogInformation($"Claim añadido en refresh token: permiso={permiso}");
                }
                else
                {
                    //_logger.LogWarning($"Permiso vacío encontrado para el usuario {credencialesUsuario.Id}.");
                }
            }
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

            return new DTOLoginResponse()
            {
                Id = credencialesUsuario.Id,
                Token = tokenString,
                Rol = credencialesUsuario.IdRolNavigation.Nombre,
            };
        }
    }
}
