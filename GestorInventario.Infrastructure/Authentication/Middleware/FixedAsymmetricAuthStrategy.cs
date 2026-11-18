using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace GestorInventario.Infrastructure.Authentication.Middleware
{
    public class FixedAsymmetricAuthStrategy : AuthenticationStrategyBase
    {
        private readonly ILogger<FixedAsymmetricAuthStrategy> _logger;

        public FixedAsymmetricAuthStrategy( ITokenService tokenService, IUserRepository userRepository, 
            IRefreshTokenService refreshTokenStrategy, IJwtTokenSettings jwtTokenSettings , 
            ILogger<FixedAsymmetricAuthStrategy> logger)
        :base(tokenService, userRepository, refreshTokenStrategy,jwtTokenSettings)
        {
         
            _logger = logger;
        }
        protected override ILogger GetLogger() => _logger;
       

        protected override async Task<ClaimsPrincipal?> ValidateAccessTokenAsync(string token)
        {
            try
            {
                var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("El token ha expirado");
                    return null ;
                }
                var rsa = _jwtTokenSettings.ObtenerPublicKeyFixed();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa.ExportParameters(false)),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtTokenSettings.ObtenerIssuer(),
                    ValidateAudience = true,
                    ValidAudience = _jwtTokenSettings.ObtenerAudience(),
                    ValidateLifetime = true
                };
                var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
                _logger.LogInformation("Claims válidos para {Claims}",
                    string.Join(", ", jwtToken.Claims.Select(c => $"{c.Type}={c.Value}")));
                return principal;
            }
            catch (InvalidOperationException ex)
            {
               
                _logger.LogError(ex, "Error de configuración de clave pública");
                return null;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Token inválido");
                return  null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al validar el token");
                return  null;
            }
        }
        protected override async Task<bool> ValidateRefreshTokenAsync(string refreshToken)
        {  try
            {
                var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(refreshToken);
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("Refresh token expirado");
                    return false;
                }

                var rsa = _jwtTokenSettings.ObtenerPublicKeyFixed();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa.ExportParameters(false)),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtTokenSettings.ObtenerIssuer(),
                    ValidateAudience = true,
                    ValidAudience = _jwtTokenSettings.ObtenerAudience(),
                    ValidateLifetime = true
                };

                new JwtSecurityTokenHandler().ValidateToken(refreshToken, validationParameters, out _);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error de configuración de clave pública al validar refresh");
                return false;
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Refresh token inválido");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al validar refresh");
                return false;
            }
        }
    }
}