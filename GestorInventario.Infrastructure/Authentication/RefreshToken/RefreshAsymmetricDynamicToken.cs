using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Interfaces.Caching;

using GestorInventario.Domain.Models;
using GestorInventario.Infrastructure.Services;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.Authentication.RefreshToken
{
    public class RefreshAsymmetricDynamicToken : BaseRefreshTokenStrategy
    {
        private readonly IHybridCacheService _cache;

        public RefreshAsymmetricDynamicToken(
            IJwtTokenSettings claimsBuilder,
            IHybridCacheService cache)
            : base(claimsBuilder)
        {
            _cache = cache;
        }

        protected override async Task<SigningCredentials> GetSigningCredentialsAsync(Usuarios usuario)
        {
            using var rsa = RSA.Create(2048);
            var privateKey = rsa.ExportParameters(true);
            var publicKey = rsa.ExportParameters(false);

            await _cache.SetStringAsync(
                $"{usuario.Id}PublicKeyRefresco",
                JsonConvert.SerializeObject(publicKey),
                TimeSpan.FromDays(30));

            return new SigningCredentials(
                new RsaSecurityKey(privateKey) { KeyId = usuario.Id.ToString() },
                SecurityAlgorithms.RsaSha256);
        }
    }
}