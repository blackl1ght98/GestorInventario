
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GestorInventario.Application.Services.Authentication.Strategies.Middleware
{
    public class SymmetricAuthStrategy : IAuthenticationMiddlewareStrategy
    {
        private readonly IConfiguration _configuration;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenGenerator _refreshTokenStrategy;
        private readonly TokenClaimsBuilder _tokenClaimsBuilder;
        public SymmetricAuthStrategy(IConfiguration configuration, ITokenGenerator tokenGenerator, IUserRepository userRepository, IRefreshTokenGenerator refreshTokenStrategy, TokenClaimsBuilder tokenClaimsBuilder)
        {
            _configuration = configuration;
            _tokenGenerator = tokenGenerator;
            _userRepository = userRepository;
            _refreshTokenStrategy = refreshTokenStrategy;
            _tokenClaimsBuilder = tokenClaimsBuilder;
        }

        public async Task ProcessAuthentication(HttpContext context,  Func<Task> next)
        {
            
            try
            {
               
                var secret = Environment.GetEnvironmentVariable("ClaveJWT") ?? _configuration["ClaveJWT"];
                if (string.IsNullOrEmpty(secret))
                {
                   
                    throw new InvalidOperationException("La clave JWT es requerida.");
                }

                // Recuperar cookies y servicios necesarios
                var token = context.Request.Cookies["auth"];
                var refreshToken = context.Request.Cookies["refreshToken"];


                // Validar el token principal
                if (!string.IsNullOrEmpty(token))
                {
                    var (jwtToken, principal) = await ValidateToken(token, secret, _configuration);
                    if (jwtToken != null && principal != null)
                    {
                        // Establecer el ClaimsPrincipal en HttpContext.User
                        context.User = principal;
                       
                    }
                    else if (!string.IsNullOrEmpty(refreshToken))
                    {
                        await HandleExpiredToken(context, refreshToken, secret,_configuration,  _tokenGenerator, _userRepository, _refreshTokenStrategy);
                    }
                }
                else if (!string.IsNullOrEmpty(refreshToken))
                {
                    await HandleExpiredToken(context, refreshToken, secret,_configuration,_tokenGenerator, _userRepository, _refreshTokenStrategy);
                }
               
            }
            catch (Exception ex)
            {
                
            }

            await next();
        }

        private async Task<(JwtSecurityToken?, ClaimsPrincipal?)> ValidateToken(string token, string secret,IConfiguration configuration)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var jwtToken = handler.ReadJwtToken(token);
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                   
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
                var tokenPayload = jwtToken.Claims.Select(c => $"{c.Type}: {c.Value}");
               

                return (jwtToken, principal);
            }
            catch (Exception ex)
            {
               
                return (null, null);
            }
        }

        private async Task HandleExpiredToken(HttpContext context, string refreshToken, string secret, IConfiguration configuration,
            ITokenGenerator tokenService, IUserRepository utility, IRefreshTokenGenerator refreshTokenMethod)
        {
            var refreshTokenValid = await ValidateRefreshToken(refreshToken, secret,configuration);
            if (!refreshTokenValid)
            {
              
                RedirectToLogin(context);
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(refreshToken);
            var userId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
               
                RedirectToLogin(context);
                return;
            }

            if (!int.TryParse(userId, out var userIdParsed))
            {
                
                RedirectToLogin(context);
                return;
            }

            var user = await utility.ObtenerUsuarioPorId(userIdParsed);
            if (user == null)
            {
                
                RedirectToLogin(context);
                return;
            }

            var newAccessToken = await tokenService.GenerateTokenAsync(user);
            var newRefreshToken = await refreshTokenMethod.GenerateTokenAsync(user);
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

           
        }

        private async Task<bool> ValidateRefreshToken(string refreshToken, string secret, IConfiguration configuration)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(refreshToken);
                if (token.ValidTo < DateTime.UtcNow)
                {
                  
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
            catch (Exception ex)
            {
               
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