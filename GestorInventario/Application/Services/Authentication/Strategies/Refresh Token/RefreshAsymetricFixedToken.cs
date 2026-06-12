using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class RefreshAsymetricFixedToken : IRefreshTokenStrategy
    {
        private readonly GestorInventarioContext _context;
        private readonly ITokenStrategyFactory _tokenStrategyFactory;
        private readonly IConfiguration _configuration;

        public RefreshAsymetricFixedToken(GestorInventarioContext context, ITokenStrategyFactory tokenStrategyFactory, IConfiguration configuration)
        {
            _context = context;
            _tokenStrategyFactory = tokenStrategyFactory;
            _configuration = configuration;
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
            var privateKeyFixed = Environment.GetEnvironmentVariable("PrivateKey")
                                      ?? _configuration["JWT:PrivateKey"];

            if (string.IsNullOrEmpty(privateKeyFixed))
                throw new InvalidOperationException("La clave privada no está configurada.");


            using var rsaFixed = RSA.Create();
            rsaFixed.FromXmlString(privateKeyFixed);
            var rsaSecurityKeyFixed = new RsaSecurityKey(rsaFixed.ExportParameters(true));
            SigningCredentials signingCredentials = new SigningCredentials(rsaSecurityKeyFixed, SecurityAlgorithms.RsaSha256);

            var refreshToken = new JwtSecurityToken(
               issuer: strategy.ObtenerIssuer(),
               audience: strategy.ObtenerAudience(),
               claims: claims,
               expires: DateTime.UtcNow.AddHours(24),
               signingCredentials: signingCredentials
           );

            return new JwtSecurityTokenHandler().WriteToken(refreshToken);
        }
    }
}
