using System.Security.Cryptography;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Domain.Models;
using GestorInventario.Infrastructure.Services;
using Microsoft.IdentityModel.Tokens;

namespace GestorInventario.Infrastructure.Authentication.AccessToken
{
    public class AsymmetricFixedTokenStrategy : BaseTokenStrategy
    {
        public AsymmetricFixedTokenStrategy(IJwtTokenSettings jwtTokenSettings)
            : base( jwtTokenSettings) { }

        protected override Task<SigningCredentials> GetSigningCredentialsAsync(Usuario usuario)
        {
            var privateKeyXml = _jwtTokenSettings.ObtenerClavePrivada();

            if (string.IsNullOrEmpty(privateKeyXml))
                throw new InvalidOperationException("La clave privada no está configurada.");

            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKeyXml);

            var credentials = new SigningCredentials(
                new RsaSecurityKey(rsa.ExportParameters(true))
                {
                    KeyId = usuario.Id.ToString()
                },
                SecurityAlgorithms.RsaSha256);

            return Task.FromResult(credentials);
        }
    }
}