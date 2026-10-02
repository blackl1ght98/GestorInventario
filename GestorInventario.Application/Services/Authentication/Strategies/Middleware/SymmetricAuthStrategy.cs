using GestorInventario.Interfaces.Application.Services.Authentication.Jwt;
using GestorInventario.Interfaces.Application.Services.Authentication.Strategies.Middleware;
using GestorInventario.Interfaces.Application.Services.Authentication.TokenGeneration.Generators;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GestorInventario.Application.Services.Authentication.Strategies.Middleware
{
    public class SymmetricAuthStrategy : IAuthenticationMiddlewareStrategy
    {
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenGenerator _refreshTokenStrategy;
        private readonly IJwtTokenSettings _tokenClaimsBuilder;
        private readonly ILogger<SymmetricAuthStrategy> _logger;

        public SymmetricAuthStrategy(
            ITokenGenerator tokenGenerator,
            IUserRepository userRepository,
            IRefreshTokenGenerator refreshTokenStrategy,
            IJwtTokenSettings tokenClaimsBuilder,
            ILogger<SymmetricAuthStrategy> logger)
        {
            _tokenGenerator = tokenGenerator;
            _userRepository = userRepository;
            _refreshTokenStrategy = refreshTokenStrategy;
            _tokenClaimsBuilder = tokenClaimsBuilder;
            _logger = logger;
        }

        public async Task ProcessAuthentication(HttpContext context, Func<Task> next)
        {
            try
            {
                var secret = _tokenClaimsBuilder.ObtenerClaveJWT();
                if (string.IsNullOrEmpty(secret))
                {
                    throw new InvalidOperationException("La clave JWT es requerida.");
                }

                var token = context.Request.Cookies["auth"];
                var refreshToken = context.Request.Cookies["refreshToken"];

                if (!string.IsNullOrEmpty(token))
                {
                    var (jwtToken, principal) = await ValidateToken(token, secret);
                    if (jwtToken != null && principal != null)
                    {
                        context.User = principal;
                    }
                    else if (!string.IsNullOrEmpty(refreshToken))
                    {
                        await HandleExpiredToken(context, refreshToken, secret);
                    }
                }
                else if (!string.IsNullOrEmpty(refreshToken))
                {
                    await HandleExpiredToken(context, refreshToken, secret);
                }
                else
                {
                    _logger.LogInformation("No se encontraron tokens en las cookies para la ruta {Path}", context.Request.Path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el middleware de autenticación simétrica para la ruta {Path}", context.Request.Path);
            }

            await next();
        }

        private async Task<(JwtSecurityToken?, ClaimsPrincipal?)> ValidateToken(string token, string secret)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var jwtToken = handler.ReadJwtToken(token);
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("Token expirado");
                    return (null, null);
                }

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateIssuer = true,
                    ValidIssuer = _tokenClaimsBuilder.ObtenerIssuer(),
                    ValidateAudience = true,
                    ValidAudience = _tokenClaimsBuilder.ObtenerAudience(),
                    ValidateLifetime = true
                };

                var principal = handler.ValidateToken(token, validationParameters, out _);

                _logger.LogInformation("Claims válidos para {Claims}",
                    string.Join(", ", jwtToken.Claims.Select(c => $"{c.Type}={c.Value}")));

                return (jwtToken, principal);
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

        private async Task HandleExpiredToken(HttpContext context, string refreshToken, string secret)
        {
            var refreshTokenValid = await ValidateRefreshToken(refreshToken, secret);
            if (!refreshTokenValid)
            {
                _logger.LogError("Refresh token no válido");
                RedirectToLogin(context);
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(refreshToken);
            var userId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("No se encontró userId en el refresh token");
                RedirectToLogin(context);
                return;
            }

            if (!int.TryParse(userId, out var userIdParsed))
            {
                _logger.LogError("El userId {UserId} no es válido", userId);
                RedirectToLogin(context);
                return;
            }

            var user = await _userRepository.ObtenerUsuarioPorId(userIdParsed);
            if (user == null)
            {
                _logger.LogError("Usuario {UserId} no encontrado", userIdParsed);
                RedirectToLogin(context);
                return;
            }

            var newAccessToken = await _tokenGenerator.GenerateTokenAsync(user);
            var newRefreshToken = await _refreshTokenStrategy.GenerateTokenAsync(user);
            var minutos = _tokenClaimsBuilder.ObtenerDuracionAccessTokenMinutos();
            var horas = _tokenClaimsBuilder.ObtenerDuracionRefreshTokenHoras();

            context.Response.Cookies.Append("auth", newAccessToken.Token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = true,
                Expires = DateTime.UtcNow.AddMinutes(minutos)
            });

            context.Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = true,
                Expires = DateTime.UtcNow.AddHours(horas)
            });

            _logger.LogInformation("Tokens generados con éxito para usuario {UserId}", userIdParsed);
        }

        private async Task<bool> ValidateRefreshToken(string refreshToken, string secret)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(refreshToken);
                if (token.ValidTo < DateTime.UtcNow)
                {
                    _logger.LogWarning("Refresh token expirado");
                    return false;
                }

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateIssuer = true,
                    ValidIssuer = _tokenClaimsBuilder.ObtenerIssuer(),
                    ValidateAudience = true,
                    ValidAudience = _tokenClaimsBuilder.ObtenerAudience(),
                    ValidateLifetime = true
                };

                handler.ValidateToken(refreshToken, validationParameters, out _);
                return true;
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

            if (!context.Request.Path.StartsWithSegments("/Auth/Login"))
            {
                context.Response.Redirect("/Auth/Login");
            }
        }
    }
}