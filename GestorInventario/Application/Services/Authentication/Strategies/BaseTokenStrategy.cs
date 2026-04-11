using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using System.Security.Claims;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public abstract class BaseTokenStrategy : ITokenStrategy
    {
        protected readonly IConfiguration _configuration;
        protected readonly GestorInventarioContext _context;

        protected BaseTokenStrategy(IConfiguration configuration, GestorInventarioContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public abstract Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario);

        // Método protegido reutilizable
        // Cambiamos a protected internal para que RefreshTokenMethod pueda usarlos
        protected internal List<Claim> CrearClaims(Usuario usuario)
        {
            return new List<Claim>
        {
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.IdRolNavigation?.Nombre ?? "Usuario"),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString())
        };
        }

        protected internal string ObtenerIssuer() =>
            Environment.GetEnvironmentVariable("JwtIssuer") ?? _configuration["JwtIssuer"];

        protected internal string ObtenerAudience() =>
            _configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JwtAudience");
    }
}
