
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies.Middleware
{
    public class FixedAsymmetricAuthStrategy : IAuthenticationMiddlewareStrategy
    {
        private readonly IConfiguration _configuration;
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenGenerator _refreshTokenStrategy;
        private readonly TokenClaimsBuilder _tokenClaimsBuilder;


        public FixedAsymmetricAuthStrategy(IConfiguration configuration, ITokenGenerator tokenGenerator, IUserRepository userRepository, IRefreshTokenGenerator refreshTokenStrategy, TokenClaimsBuilder builder )
        {
            _tokenGenerator = tokenGenerator;
            _userRepository = userRepository;
            _refreshTokenStrategy = refreshTokenStrategy;
            _configuration = configuration;
            _tokenClaimsBuilder = builder;
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
                    var (jwtToken, principal) = await ValidateToken(token, _configuration);
                    if (jwtToken != null && principal != null)
                    {
                        context.User = principal;
                       
                    }
                    else if (!string.IsNullOrEmpty(refreshToken))
                    {
                        await HandleExpiredToken(context, refreshToken,_configuration,  _tokenGenerator, _userRepository, _refreshTokenStrategy);
                    }
                }
                else if (!string.IsNullOrEmpty(refreshToken))
                {
                    await HandleExpiredToken(context, refreshToken,_configuration, _tokenGenerator, _userRepository, _refreshTokenStrategy);
                }
                else
                {
                   // _logger.LogInformation("No se encontraron tokens en las cookies para la ruta {Path}", context.Request.Path);
                }
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, "Error en el middleware de autenticación para la ruta {Path}", context.Request.Path);
            }

            await next();
        }

        private async Task<(JwtSecurityToken?, ClaimsPrincipal?)> ValidateToken(string token,IConfiguration configuration)
        {
            var handler = new JwtSecurityTokenHandler();
            var logger = log4net.LogManager.GetLogger(typeof(FixedAsymmetricAuthStrategy));
            try
            {
                var jwtToken = handler.ReadJwtToken(token);
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                  logger.Error("El token ha expirado");
                    return (null, null);
                }

                var publicKey = _tokenClaimsBuilder.ObtenerPublicKeyFixed();
                if (string.IsNullOrEmpty(publicKey))
                {
                   logger.Error("La clave pública JWT no es valida");
                    return (null, null);
                }

                var rsa = new RSACryptoServiceProvider();
                try
                {
                    rsa.FromXmlString(publicKey);
                }
                catch (Exception ex)
                {
                   logger.Error("Error al cargar la clave pública RSA desde la configuración.",ex);
                    return (null, null);
                }

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa),
                    ValidateIssuer = true,
                    ValidIssuer = _tokenClaimsBuilder.ObtenerIssuer(),
                    ValidateAudience = true,
                    ValidAudience = _tokenClaimsBuilder.ObtenerAudience(),
                    ValidateLifetime = true
                };

                var principal = handler.ValidateToken(token, validationParameters, out _);
                var tokenPayload = jwtToken.Claims.Select(c => $"{c.Type}: {c.Value}");
              logger.Info("Claims validos");

                return (jwtToken, principal);
            }
            catch (Exception ex)
            {
              
                logger.Error("Error al validar el token",ex);
                return (null, null);
            }
        }

        private async Task HandleExpiredToken(HttpContext context, string refreshToken,IConfiguration configuration,
            ITokenGenerator tokenService, IUserRepository utility, IRefreshTokenGenerator refreshTokenMethod)
        {
            var refreshTokenValid = await ValidateRefreshToken(refreshToken,configuration);
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

            var user = await utility.ObtenerUsuarioPorId(userIdParsed);
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

        private async Task<bool> ValidateRefreshToken(string refreshToken,IConfiguration configuration)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(refreshToken);
                var logger = log4net.LogManager.GetLogger(typeof(FixedAsymmetricAuthStrategy));
                if (token.ValidTo < DateTime.UtcNow)
                {
                  logger.Error("Refresh token expirado ");
                    return false;
                }

                var publicKey = _tokenClaimsBuilder.ObtenerPublicKeyFixed();
                if (string.IsNullOrEmpty(publicKey))
                {
                   logger.Error("La clave pública JWT no está configurada ");
                    return false;
                }

                var rsa = new RSACryptoServiceProvider();
                try
                {
                    rsa.FromXmlString(publicKey);
                }
                catch (Exception ex)
                {
                  logger.Error( "Error al cargar la clave pública RSA desde la configuración.",ex);
                    return false;
                }

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa),
                    ValidateIssuer = true,
                    ValidIssuer = _tokenClaimsBuilder.ObtenerIssuer(),
                    ValidateAudience = true,
                    ValidAudience = _tokenClaimsBuilder.ObtenerAudience(),
                    ValidateLifetime = true
                };

                handler.ValidateToken(refreshToken, validationParameters, out _);
               logger.Info("Refresh token validado correctamente ");
                return true;
            }
            catch (Exception ex)
            {
                var logger = log4net.LogManager.GetLogger(typeof(FixedAsymmetricAuthStrategy));
                logger.Error("Error al validar el token", ex);
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