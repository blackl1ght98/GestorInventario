
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

        public SymmetricAuthStrategy(IConfiguration configuration, ITokenGenerator tokenGenerator, IUserRepository userRepository, IRefreshTokenGenerator refreshTokenStrategy)
        {
            _configuration = configuration;
            _tokenGenerator = tokenGenerator;
            _userRepository = userRepository;
            _refreshTokenStrategy = refreshTokenStrategy;
        }

        public async Task ProcessAuthentication(HttpContext context,  Func<Task> next)
        {
            
            try
            {
                var configuration = context.RequestServices.GetService<IConfiguration>();
                var secret = Environment.GetEnvironmentVariable("ClaveJWT") ?? configuration["ClaveJWT"];
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
                    var (jwtToken, principal) = await ValidateToken(token, secret, configuration);
                    if (jwtToken != null && principal != null)
                    {
                        // Establecer el ClaimsPrincipal en HttpContext.User
                        context.User = principal;
                       
                    }
                    else if (!string.IsNullOrEmpty(refreshToken))
                    {
                        await HandleExpiredToken(context, refreshToken, secret,configuration,  _tokenGenerator, _userRepository, _refreshTokenStrategy);
                    }
                }
                else if (!string.IsNullOrEmpty(refreshToken))
                {
                    await HandleExpiredToken(context, refreshToken, secret,configuration,_tokenGenerator, _userRepository, _refreshTokenStrategy);
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
                    ValidIssuer = configuration["JwtIssuer"] ?? Environment.GetEnvironmentVariable("JWT_ISSUER"),
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
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

            context.Response.Cookies.Append("auth", newAccessToken.Token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,              
                Secure = true,
                Expires = DateTime.UtcNow.AddMinutes(10)
            });

            context.Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
               
                Secure = true,
                Expires = DateTime.UtcNow.AddDays(7)
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
                    ValidIssuer = configuration["JwtIssuer"] ?? Environment.GetEnvironmentVariable("JWT_ISSUER"),
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
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