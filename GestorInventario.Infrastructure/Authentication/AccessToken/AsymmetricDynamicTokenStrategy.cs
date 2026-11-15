using System.Security.Cryptography;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Interfaces.Caching;
using GestorInventario.Domain.Models;
using GestorInventario.Infrastructure.Services;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.Authentication.AccessToken
{
    public class AsymmetricDynamicTokenStrategy : BaseTokenStrategy
    {
        private readonly IHybridCacheService _cache;
   

        public AsymmetricDynamicTokenStrategy(
            IJwtTokenSettings jwtTokenSettings,
            IHybridCacheService cache)
            : base( jwtTokenSettings)
        {
            _cache = cache;
           
        }

        protected override async Task<SigningCredentials> GetSigningCredentialsAsync(Usuario usuario)
        {
            // Generación dinámica del par RSA
            using var rsa = RSA.Create(2048);
            var privateKey = rsa.ExportParameters(true);
            var publicKey = rsa.ExportParameters(false);

            // Guardamos la pública en caché de forma asíncrona
            await _cache.SetStringAsync(
                $"{usuario.Id}PublicKey",
                JsonConvert.SerializeObject(publicKey));

            // Devolvemos las credenciales con la clave privada
            return new SigningCredentials(
                new RsaSecurityKey(privateKey) { KeyId = usuario.Id.ToString() },
                SecurityAlgorithms.RsaSha256);
        }

    }
}