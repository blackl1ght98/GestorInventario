using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Infrastructure.Interfaces.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Infrastructure.Authentication.Middleware
{
    public abstract class AuthenticationStrategyBase : IAuthenticationMiddlewareStrategy
    {
        private readonly ITokenService _tokenService;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenService _refreshTokenService;
        protected readonly IJwtTokenSettings _jwtTokenSettings;

        protected AuthenticationStrategyBase(
            ITokenService tokenService,
            IUserRepository userRepository,
            IRefreshTokenService refreshTokenService,
            IJwtTokenSettings jwtTokenSettings)
        {
            _tokenService = tokenService;
            _userRepository = userRepository;
            _refreshTokenService = refreshTokenService;
            _jwtTokenSettings = jwtTokenSettings;
        }

        public async Task ProcessAuthentication(HttpContext context, Func<Task> next)
        {
            try
            {
                var token = context.Request.Cookies["auth"];
                var refreshToken = context.Request.Cookies["refreshToken"];

                
                if (!string.IsNullOrEmpty(token))
                {
                    var principal = await ValidateAccessTokenAsync(token);
                    if (principal != null)
                    {
                        context.User = principal;
                    }
                    else if (!string.IsNullOrEmpty(refreshToken))
                    {
                        await HandleExpiredTokenAsync(context, refreshToken);
                    }
                }
                else if (!string.IsNullOrEmpty(refreshToken))
                {
                    await HandleExpiredTokenAsync(context, refreshToken);
                }
            }
            catch (Exception ex)
            {
                GetLogger().LogError(ex, "Error en el middleware de autenticación para la ruta {Path}", context.Request.Path);
            }

            await next();
        }

        protected async Task HandleExpiredTokenAsync(HttpContext context, string refreshToken)
        {
            var logger = GetLogger();
            
            if (!await ValidateRefreshTokenAsync(refreshToken))
            {
                logger.LogError("Refresh token no válido o expirado");
                RedirectToLogin(context);
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(refreshToken);
            var userIdStr = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
            {
                logger.LogError("No se encontró un userId válido en el refresh token");
                RedirectToLogin(context);
                return;
            }

            var user = await _userRepository.ObtenerUsuarioPorId(userId);
            if (user == null)
            {
                logger.LogError("Usuario {UserId} no encontrado en base de datos", userId);
                RedirectToLogin(context);
                return;
            }

            var newAccessToken = await _tokenService.GenerarToken(user);
            var newRefreshToken = await _refreshTokenService.GenerateTokenAsync(user);
            
            var minutos = _jwtTokenSettings.ObtenerDuracionAccessTokenMinutos();
            var horas = _jwtTokenSettings.ObtenerDuracionRefreshTokenHoras();

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
            var principal = await ValidateAccessTokenAsync(newAccessToken.Token);
            if (principal != null)
            {
                context.User = principal;
            }
            logger.LogInformation("Tokens regenerados con éxito para usuario {UserId}", userId);
        }

        protected void RedirectToLogin(HttpContext context)
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

        // Métodos abstractos que cada estrategia implementará según su criptografía (Simétrica / RSA Fijo / RSA Dinámico)
        protected abstract Task<ClaimsPrincipal?> ValidateAccessTokenAsync(string token);
        protected abstract Task<bool> ValidateRefreshTokenAsync(string refreshToken);
        protected abstract ILogger GetLogger();
    }
}