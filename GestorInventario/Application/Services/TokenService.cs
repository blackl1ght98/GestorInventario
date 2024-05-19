using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestorInventario.Domain.Models;
using GestorInventario.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenService(IConfiguration configuration, GestorInventarioContext context, IHttpContextAccessor httpContext)
        {
            _configuration = configuration;
            _context = context;
            _httpContextAccessor = httpContext;
        }
        //CONFIGURACION CLAVE SIMETRICA
        //public async Task<DTOLoginResponse> GenerarToken(Usuario credencialesUsuario)
        //{

        //    var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);



        //    var claims = new List<Claim>()
        //    {
        //         new Claim(ClaimTypes.Email, credencialesUsuario.Email),
        //         new Claim(ClaimTypes.Role, credencialesUsuario.IdRolNavigation.Nombre),

        //         new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())

        //    };

        //    var clave = _configuration["ClaveJWT"];
        //    var claveKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));
        //    var signinCredentials = new SigningCredentials(claveKey, SecurityAlgorithms.HmacSha256);
        //    var securityToken = new JwtSecurityToken(
        //        issuer: _configuration["JwtIssuer"],
        //        audience: _configuration["JwtAudience"],
        //        claims: claims,
        //        expires: DateTime.Now.AddDays(30),
        //        signingCredentials: signinCredentials);
        //    var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);

        //    return new DTOLoginResponse()
        //    {
        //        Id = credencialesUsuario.Id,
        //        Token = tokenString,
        //        Rol = credencialesUsuario.IdRolNavigation.Nombre,

        //    };
        //}
        //CONFIGURACION CLAVE ASIMETRICA FIJA
        //public async Task<DTOLoginResponse> GenerarToken(Usuario credencialesUsuario)
        //{
        //    var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);

        //    var claims = new List<Claim>()
        //    {
        //        new Claim(ClaimTypes.Email, credencialesUsuario.Email),
        //        new Claim(ClaimTypes.Role, credencialesUsuario.IdRolNavigation.Nombre),
        //        new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())
        //    };

        //    // Carga la clave privada desde la configuración
        //    var privateKey = _configuration["Jwt:PrivateKey"];

        //    // Convierte la clave privada a formato RSA
        //    var rsa = new RSACryptoServiceProvider();
        //    rsa.FromXmlString(privateKey);

        //    // Crea las credenciales de firma con la clave privada
        //    var signinCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

        //    var securityToken = new JwtSecurityToken(
        //        issuer: _configuration["JwtIssuer"],
        //        audience: _configuration["JwtAudience"],
        //        claims: claims,
        //        expires: DateTime.Now.AddDays(1),
        //        signingCredentials: signinCredentials);
        //    var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);

        //    return new DTOLoginResponse()
        //    {
        //        Id = credencialesUsuario.Id,
        //        Token = tokenString,
        //        Rol = credencialesUsuario.IdRolNavigation.Nombre,
        //    };
        //}
        //CONFIGURACION CLAVE ASIMETRICA DINAMICA SOLO CAPAZ DE MANEJAR 1 SESION
        public async Task<DTOLoginResponse> GenerarToken(Usuario credencialesUsuario)
        {
            var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                new Claim(ClaimTypes.Role, credencialesUsuario.IdRolNavigation.Nombre),
                new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())
            };

            // Genera un nuevo par de claves RSA
            var rsa = new RSACryptoServiceProvider(2048);
            var privateKey = rsa.ToXmlString(true);
            var publicKey = rsa.ToXmlString(false);

            // Guarda las claves en la configuración
            _httpContextAccessor.HttpContext.Response.Cookies.Append("PrivateKey", privateKey, new CookieOptions { HttpOnly = true, IsEssential = true, Secure = true, SameSite = SameSiteMode.Strict });
            _httpContextAccessor.HttpContext.Response.Cookies.Append("PublicKey", publicKey, new CookieOptions { HttpOnly = true, IsEssential = true, Secure = true, SameSite = SameSiteMode.Strict });

            // Crea las credenciales de firma con la clave privada
            var signinCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

            var securityToken = new JwtSecurityToken(
                issuer: _configuration["JwtIssuer"],
                audience: _configuration["JwtAudience"],
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
