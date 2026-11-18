using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace GestorInventario.Infrastructure.Authentication.Middleware
{
    public class SymmetricAuthStrategy : AuthenticationStrategyBase
    {
        private readonly ILogger<SymmetricAuthStrategy> _logger;

        public SymmetricAuthStrategy(
            ITokenService tokenService,
            IUserRepository userRepository,
            IRefreshTokenService refreshTokenStrategy,
            IJwtTokenSettings jwtTokenSettings,
            ILogger<SymmetricAuthStrategy> logger)
            : base(tokenService, userRepository, refreshTokenStrategy, jwtTokenSettings)
        {
            _logger = logger;
        }

        protected override ILogger GetLogger() => _logger;

        protected override async Task<ClaimsPrincipal?> ValidateAccessTokenAsync(string token)
        {
            var secret = _jwtTokenSettings.ObtenerClaveJWT();
            if (string.IsNullOrEmpty(secret))
                throw new InvalidOperationException("La clave JWT es requerida.");

            return ValidateTokenInternal(token, secret);
        }

        protected override async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {
            var secret = _jwtTokenSettings.ObtenerClaveJWT();
            if (string.IsNullOrEmpty(secret)) return false;

            try
            {
                ValidateTokenInternal(refreshToken, secret);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private ClaimsPrincipal? ValidateTokenInternal(string token, string secret)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            if (jwtToken.ValidTo < DateTime.UtcNow) return null;

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = true,
                ValidIssuer = _jwtTokenSettings.ObtenerIssuer(),
                ValidateAudience = true,
                ValidAudience = _jwtTokenSettings.ObtenerAudience(),
                ValidateLifetime = true
            };

            return handler.ValidateToken(token, validationParameters, out _);
        }
    }
}