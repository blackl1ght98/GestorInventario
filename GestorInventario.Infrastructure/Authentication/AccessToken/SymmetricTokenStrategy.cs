using System.Text;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Domain.Models;
using GestorInventario.Infrastructure.Services;
using Microsoft.IdentityModel.Tokens;

namespace GestorInventario.Infrastructure.Authentication.AccessToken
{
    public class SymmetricTokenStrategy : BaseTokenStrategy
    {
        public SymmetricTokenStrategy( IJwtTokenSettings claimsBuilder)
            : base( claimsBuilder) { }

        protected override Task<SigningCredentials> GetSigningCredentialsAsync(Usuario usuario)
        {
            var clave = _jwtTokenSettings.ObtenerClaveJWT();

            if (string.IsNullOrEmpty(clave))
                throw new InvalidOperationException("La clave JWT no está configurada.");

            if (Encoding.UTF8.GetByteCount(clave) < 32)
                throw new InvalidOperationException("La clave JWT debe tener al menos 32 bytes.");

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
                SecurityAlgorithms.HmacSha256);

            return Task.FromResult(credentials);
        }
    }
}