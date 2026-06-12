using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class RefreshSymmetricToken : IRefreshTokenStrategy
    {
        private readonly GestorInventarioContext _context;
        private readonly ITokenStrategyFactory _tokenStrategyFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RefreshSymmetricToken> _logger;

        public RefreshSymmetricToken(GestorInventarioContext context, ITokenStrategyFactory tokenStrategyFactory, IConfiguration configuration, ILogger<RefreshSymmetricToken> logger)
        {
            _context = context;
            _tokenStrategyFactory = tokenStrategyFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GenerarTokenRefresco(Usuario credencialesUsuario)
        {
            var usuarioDB = await _context.Usuarios
               .Include(u => u.IdRolNavigation)
               .FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);

            if (usuarioDB == null)
            {
                throw new ArgumentException("El usuario no existe en la base de datos.");
            }

            var strategy = (BaseTokenStrategy)_tokenStrategyFactory.CreateStrategy();
            var claims = strategy.CrearClaims(credencialesUsuario);
            var claveJwt = _configuration["ClaveJWT"];
            if (string.IsNullOrEmpty(claveJwt))
            {
                _logger.LogError("La clave JWT no está configurada.");
                throw new InvalidOperationException("La clave JWT no está configurada.");
            }

            var key = Encoding.UTF8.GetBytes(claveJwt);
            if (key.Length < 32)
            {
                _logger.LogError("La clave JWT es demasiado corta para HMAC-SHA256.");
                throw new InvalidOperationException("La clave JWT debe tener al menos 32 bytes.");
            }

            SigningCredentials signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
             var refreshToken = new JwtSecurityToken(
                issuer: _configuration["JwtIssuer"],
                audience: _configuration["JwtAudience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(refreshToken);
        }
    }
}
