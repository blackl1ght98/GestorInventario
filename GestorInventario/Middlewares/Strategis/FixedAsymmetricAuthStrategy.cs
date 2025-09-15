using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GestorInventario.Middlewares.Strategis
{
    public class FixedAsymmetricAuthStrategy : IAuthProcessingStrategy
    {
      

        public async Task ProcessAuthentication(HttpContext context, WebApplicationBuilder builder, Func<Task> next)
        {
            try
            {
                // Recuperar cookies y servicios necesarios
                var token = context.Request.Cookies["auth"];
                var refreshToken = context.Request.Cookies["refreshToken"];
                var tokenService = context.RequestServices.GetRequiredService<ITokenGenerator>();
                var userService = context.RequestServices.GetRequiredService<IAdminRepository>();
                var refreshTokenMethod = context.RequestServices.GetRequiredService<IRefreshTokenMethod>();

                // Validar el token principal
                if (!string.IsNullOrEmpty(token))
                {
                    var (jwtToken, principal) = await ValidateToken(token, builder);
                    if (jwtToken != null && principal != null)
                    {
                        context.User = principal;
                       
                    }
                    else if (!string.IsNullOrEmpty(refreshToken))
                    {
                        await HandleExpiredToken(context, refreshToken, builder, tokenService, userService, refreshTokenMethod);
                    }
                }
                else if (!string.IsNullOrEmpty(refreshToken))
                {
                    await HandleExpiredToken(context, refreshToken, builder, tokenService, userService, refreshTokenMethod);
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

        private async Task<(JwtSecurityToken?, ClaimsPrincipal?)> ValidateToken(string token, WebApplicationBuilder builder)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var jwtToken = handler.ReadJwtToken(token);
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                   // _logger.LogWarning("Token ha expirado para la ruta {Path}", builder.WebHost);
                    return (null, null);
                }

                var publicKey = builder.Configuration["Jwt:PublicKey"];
                if (string.IsNullOrEmpty(publicKey))
                {
                   // _logger.LogError("La clave pública JWT no está configurada en Jwt:PublicKey.");
                    return (null, null);
                }

                var rsa = new RSACryptoServiceProvider();
                try
                {
                    rsa.FromXmlString(publicKey);
                }
                catch (Exception ex)
                {
                   // _logger.LogError(ex, "Error al cargar la clave pública RSA desde la configuración.");
                    return (null, null);
                }

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa),
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JwtAudience"],
                    ValidateLifetime = true
                };

                var principal = handler.ValidateToken(token, validationParameters, out _);
                var tokenPayload = jwtToken.Claims.Select(c => $"{c.Type}: {c.Value}");
              //  _logger.LogInformation("Claims validados en el token: {Claims}", string.Join(", ", tokenPayload));

                return (jwtToken, principal);
            }
            catch (Exception ex)
            {
               // _logger.LogError(ex, "Error al validar el token para la ruta {Path}", builder.WebHost);
                return (null, null);
            }
        }

        private async Task HandleExpiredToken(HttpContext context, string refreshToken, WebApplicationBuilder builder,
            ITokenGenerator tokenService, IAdminRepository userService, IRefreshTokenMethod refreshTokenMethod)
        {
            var refreshTokenValid = await ValidateRefreshToken(refreshToken, builder);
            if (!refreshTokenValid)
            {
               // _logger.LogWarning("Refresh token no válido o expirado para la ruta {Path}", context.Request.Path);
                RedirectToLogin(context);
                return;
            }

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(refreshToken);
            var userId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
               // _logger.LogWarning("No se encontró userId en el refresh token para la ruta {Path}", context.Request.Path);
                RedirectToLogin(context);
                return;
            }

            if (!int.TryParse(userId, out var userIdParsed))
            {
               // _logger.LogWarning("El userId {UserId} no es válido para la ruta {Path}", userId, context.Request.Path);
                RedirectToLogin(context);
                return;
            }

            var (user,mensaje) = await userService.ObtenerPorId(userIdParsed);
            if (user == null)
            {
               // _logger.LogWarning("Usuario con ID {UserId} no encontrado para la ruta {Path}", userId, context.Request.Path);
                RedirectToLogin(context);
                return;
            }

            var newAccessToken = await tokenService.GenerateTokenAsync(user);
            var newRefreshToken = await refreshTokenMethod.GenerarTokenRefresco(user);

            context.Response.Cookies.Append("auth", newAccessToken.Token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Domain = "localhost",
                Secure = true,
                Expires = DateTime.UtcNow.AddMinutes(10)
            });

            context.Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Domain = "localhost",
                Secure = true,
                Expires = DateTime.UtcNow.AddDays(7)
            });

           // _logger.LogInformation("Nuevos tokens generados para el usuario {UserId} en la ruta {Path}", userId, context.Request.Path);
        }

        private async Task<bool> ValidateRefreshToken(string refreshToken, WebApplicationBuilder builder)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(refreshToken);
                if (token.ValidTo < DateTime.UtcNow)
                {
                  //  _logger.LogWarning("Refresh token expirado para la ruta {Path}", builder.WebHost);
                    return false;
                }

                var publicKey = builder.Configuration["Jwt:PublicKey"];
                if (string.IsNullOrEmpty(publicKey))
                {
                   // _logger.LogError("La clave pública JWT no está configurada en Jwt:PublicKey.");
                    return false;
                }

                var rsa = new RSACryptoServiceProvider();
                try
                {
                    rsa.FromXmlString(publicKey);
                }
                catch (Exception ex)
                {
                  //  _logger.LogError(ex, "Error al cargar la clave pública RSA desde la configuración.");
                    return false;
                }

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa),
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JwtAudience"],
                    ValidateLifetime = true
                };

                handler.ValidateToken(refreshToken, validationParameters, out _);
               // _logger.LogInformation("Refresh token validado correctamente para la ruta {Path}", builder.WebHost);
                return true;
            }
            catch (Exception ex)
            {
             //   _logger.LogError(ex, "Error al validar el refresh token para la ruta {Path}", builder.WebHost);
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
               // _logger.LogInformation("Redirigiendo a /Auth/Login desde la ruta {Path}", context.Request.Path);
                context.Response.Redirect("/Auth/Login");
            }
        }
    }
}