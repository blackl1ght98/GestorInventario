using System.IdentityModel.Tokens.Jwt;
using System.Text;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Services.Authentication;
using GestorInventario.Domain.Models;
using GestorInventario.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace GestorInventario.Infrastructure.Authentication.RefreshToken
{
    public class RefreshSymmetricToken : BaseRefreshTokenStrategy
    {
        
        private readonly ILogger<RefreshSymmetricToken> _logger;

        public RefreshSymmetricToken(
            IJwtTokenSettings claimsBuilder,     
            ILogger<RefreshSymmetricToken> logger)
            :base(claimsBuilder)
        {
           
            _logger = logger;
        }

        protected override Task<SigningCredentials> GetSigningCredentialsAsync(Usuarios usuario)
        {
            var clave = _jwtTokenSettings.ObtenerClaveJWT();

            if (string.IsNullOrEmpty(clave))
            {
                _logger.LogError("La clave JWT no está configurada.");
                throw new InvalidOperationException("La clave JWT no está configurada.");
            }

            if (Encoding.UTF8.GetByteCount(clave) < 32)
            {
                _logger.LogError("La clave JWT es demasiado corta para HMAC-SHA256.");
                throw new InvalidOperationException("La clave JWT debe tener al menos 32 bytes.");
            }

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
                SecurityAlgorithms.HmacSha256);

            return Task.FromResult(credentials);
        }
    }
}
