using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Jwt;
using GestorInventario.Interfaces.Application.Services.Authentication.Strategies.RefreshToken;
using GestorInventario.Interfaces.Application.Services.Common;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies.RefreshToken
{
    public class RefreshAsymmetricDynamicToken : IRefreshTokenStrategy
    {
        private readonly IJwtTokenSettings _claimsBuilder;
        private readonly IHybridCacheService _cache;

        public RefreshAsymmetricDynamicToken(
            IJwtTokenSettings claimsBuilder,
            IHybridCacheService cache)
        {
            _claimsBuilder = claimsBuilder;
            _cache = cache;
        }

        public async Task<string> GenerarTokenRefresco(Usuario usuario)
        {
            using var rsa = RSA.Create(2048);
            var privateKey = rsa.ExportParameters(true);
            var publicKey = rsa.ExportParameters(false);

            await _cache.SetStringAsync(
                $"{usuario.Id}PublicKeyRefresco",
                JsonConvert.SerializeObject(publicKey),
                TimeSpan.FromDays(30));

            var credentials = new SigningCredentials(
                new RsaSecurityKey(privateKey) { KeyId = usuario.Id.ToString() },
                SecurityAlgorithms.RsaSha256);
            var horas = _claimsBuilder.ObtenerDuracionRefreshTokenHoras();
            var token = new JwtSecurityToken(
                issuer: _claimsBuilder.ObtenerIssuer(),
                audience: _claimsBuilder.ObtenerAudience(),
                claims: _claimsBuilder.CrearClaims(usuario),
                expires: DateTime.UtcNow.AddHours(horas),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
