using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace GestorInventario.Application.Services.Authentication.Strategies.Middleware
{
    public class FixedAsymmetricAuthStrategy : IAuthenticationMiddlewareStrategy
    {
        
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenGenerator _refreshTokenStrategy;
        private readonly ITokenClaimsBuilder _tokenClaimsBuilder;
        private readonly ILogger<FixedAsymmetricAuthStrategy> _logger;

        public FixedAsymmetricAuthStrategy( ITokenGenerator tokenGenerator, IUserRepository userRepository, 
            IRefreshTokenGenerator refreshTokenStrategy, ITokenClaimsBuilder builder , ILogger<FixedAsymmetricAuthStrategy> logger)
        {
            _tokenGenerator = tokenGenerator;
            _userRepository = userRepository;
            _refreshTokenStrategy = refreshTokenStrategy;
          
            _tokenClaimsBuilder = builder;
            _logger = logger;
        }

        public async Task ProcessAuthentication(HttpContext context,  Func<Task> next)
        {
            try
            {
                // Recuperar cookies y servicios necesarios
                var token = context.Request.Cookies["auth"];
                var refreshToken = context.Request.Cookies["refreshToken"];
               
                // Validar el token principal
                if (!string.IsNullOrEmpty(token))
                {
                    var (jwtToken, principal) = await ValidateToken(token);
                    if (jwtToken != null && principal != null)
                    {
                        context.User = principal;
                       
                    }
                    else if (!string.IsNullOrEmpty(refreshToken))
                    {
                        await HandleExpiredToken(context, refreshToken,  _tokenGenerator, _userRepository, _refreshTokenStrategy);
                    }
                }
                else if (!string.IsNullOrEmpty(refreshToken))
                {
                    await HandleExpiredToken(context, refreshToken, _tokenGenerator, _userRepository, _refreshTokenStrategy);
                }
                else
                {
                    _logger.LogInformation("No se encontraron tokens en las cookies para la ruta {Path}", context.Request.Path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el middleware de autenticación para la ruta {Path}", context.Request.Path);
            }

            await next();
        }

        private async Task<(JwtSecurityToken?, ClaimsPrincipal?)> ValidateToken(string token)
        {
            try
            {
                var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("El token ha expirado");
                    return (null, null);
                }

                // El builder ya validó el XML y cachea el RSA. Si falla, lanza InvalidOperationException.
                var rsa = _tokenClaimsBuilder.ObtenerPublicKeyFixed();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa.ExportParameters(false)),
                    ValidateIssuer = true,
                    ValidIssuer = _tokenClaimsBuilder.ObtenerIssuer(),
                    ValidateAudience = true,
                    ValidAudience = _tokenClaimsBuilder.ObtenerAudience(),
                    ValidateLifetime = true
                };

                var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
                _logger.LogInformation("Claims válidos para {Claims}",
                    string.Join(", ", jwtToken.Claims.Select(c => $"{c.Type}={c.Value}")));

                return (jwtToken, principal);
            }
            catch (InvalidOperationException ex)
            {
                // El builder lanzó: clave no configurada o XML inválido
                _logger.LogError(ex, "Error de configuración de clave pública");
                return (null, null);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Token inválido");
                return (null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al validar el token");
                return (null, null);
            }
        }

        private async Task HandleExpiredToken(HttpContext context, string refreshToken,
            ITokenGenerator tokenService, IUserRepository repository, IRefreshTokenGenerator refreshTokenMethod)
        {
            var refreshTokenValid = await ValidateRefreshToken(refreshToken);
            var logger = log4net.LogManager.GetLogger(typeof(FixedAsymmetricAuthStrategy));
            if (!refreshTokenValid)
            {
               logger.Error("Refresh token no válido ");
                RedirectToLogin(context);
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(refreshToken);
            var userId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
               logger.Error("No se encontró userId en el refresh token ");
                RedirectToLogin(context);
                return;
            }

            if (!int.TryParse(userId, out var userIdParsed))
            {
               logger.Error("El userId {UserId} no es válido ");
                RedirectToLogin(context);
                return;
            }

            var user = await repository.ObtenerUsuarioPorId(userIdParsed);
            if (user == null)
            {
               logger.Error("Usuario no encontrado");
                RedirectToLogin(context);
                return;
            }

            var newAccessToken = await tokenService.GenerateTokenAsync(user);
            var newRefreshToken = await refreshTokenMethod.GenerateTokenAsync(user);
            var minutos = _tokenClaimsBuilder.ObtenerDuracionAccessTokenMinutos();
         
            context.Response.Cookies.Append("auth", newAccessToken.Token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
               
                Secure = true,
                Expires = DateTime.UtcNow.AddMinutes(minutos)
            });

        

           logger.Info("Tokens generados con exito");
        }

        private async Task<bool> ValidateRefreshToken(string refreshToken)
        {
            try
            {
                var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(refreshToken);
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("Refresh token expirado");
                    return false;
                }

                var rsa = _tokenClaimsBuilder.ObtenerPublicKeyFixed();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa.ExportParameters(false)),
                    ValidateIssuer = true,
                    ValidIssuer = _tokenClaimsBuilder.ObtenerIssuer(),
                    ValidateAudience = true,
                    ValidAudience = _tokenClaimsBuilder.ObtenerAudience(),
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

        private void RedirectToLogin(HttpContext context)
        {
            foreach (var cookie in context.Request.Cookies)
            {
                context.Response.Cookies.Delete(cookie.Key);
            }
            if (context.Request.Path != "/Auth/Login")
            {
             
                context.Response.Redirect("/Auth/Login");
            }
        }
    }
}