using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class RefreshAsymmetricDynamicToken : IRefreshTokenStrategy
    {
        private readonly GestorInventarioContext _context;
        private readonly ITokenStrategyFactory _tokenStrategyFactory;
        private readonly ICacheService _cache;

        public RefreshAsymmetricDynamicToken(GestorInventarioContext context, ITokenStrategyFactory tokenStrategyFactory, ICacheService cache)
        {
            _context = context;
            _tokenStrategyFactory = tokenStrategyFactory;
            _cache = cache;
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
            string cacheKey = credencialesUsuario.Id.ToString() + "PublicKeyRefresco";

            // 1. Recuperar la pública
            string? publicKeyJson = await _cache.GetStringAsync(cacheKey);

            // 2. Generamos la nueva pareja de claves
            RSAParameters privateKey;
            RSAParameters publicKey;


            using (var rsa = RSA.Create(2048))
            {
                privateKey = rsa.ExportParameters(true);
                publicKey = rsa.ExportParameters(false);

                // 3. Guardamos la pública
                var publicKeyJsonToSave = JsonConvert.SerializeObject(publicKey);

                // Usamos un tiempo de expiración de 30 días
                await _cache.SetStringAsync(cacheKey, publicKeyJsonToSave, TimeSpan.FromDays(30));
            }

            var rsaSecurityKeyDynamic = new RsaSecurityKey(privateKey)
            {
                KeyId = credencialesUsuario.Id.ToString()
            };
            SigningCredentials signingCredentials = new SigningCredentials(rsaSecurityKeyDynamic, SecurityAlgorithms.RsaSha256);
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
