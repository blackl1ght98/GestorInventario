using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class RefreshAsymmetricDynamicToken : IRefreshTokenStrategy
    {
        private readonly TokenClaimsBuilder _claimsBuilder;
        private readonly ICacheService _cache;

        public RefreshAsymmetricDynamicToken(
            TokenClaimsBuilder claimsBuilder,
            ICacheService cache)
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

            var token = new JwtSecurityToken(
                issuer: _claimsBuilder.ObtenerIssuer(),
                audience: _claimsBuilder.ObtenerAudience(),
                claims: _claimsBuilder.CrearClaims(usuario),
                expires: DateTime.UtcNow.AddHours(_claimsBuilder.ObtenerDuracionRefreshTokenHoras()),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
