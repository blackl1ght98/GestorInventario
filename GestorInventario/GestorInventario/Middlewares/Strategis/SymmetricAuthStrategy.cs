using GestorInventario.Interfaces.Application;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GestorInventario.Middlewares.Strategis
{
    public class SymmetricAuthStrategy : IAuthProcessingStrategy
    {
        public async Task ProcessAuthentication(HttpContext context, WebApplicationBuilder builder, Func<Task> next)
        {
            var secret = Environment.GetEnvironmentVariable("ClaveJWT") ?? builder.Configuration["ClaveJWT"];
            try
            {
                var token = context.Request.Cookies["auth"];
                if (!string.IsNullOrEmpty(token))
                {
                    var principal = await ValidateToken(token, secret, builder);
                    if (principal != null)
                    {
                        context.User = principal;
                        // Actualizar el token en la sesión
                        context.Session.SetString("auth", token);
                    }
                }
            }
            catch (SecurityTokenException ex)
            {
                var logger = log4net.LogManager.GetLogger(typeof(Program));
                logger.Error("Error al validar el token", ex);
            }
            catch (Exception ex)
            {
                var logger = log4net.LogManager.GetLogger(typeof(Program));
                logger.Error("Error inesperado en el middleware de autenticación", ex);
            }

            await next();
        }

        private static async Task<ClaimsPrincipal?> ValidateToken(string token, string secret, WebApplicationBuilder builder)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateIssuer = true,
                    ValidIssuer = Environment.GetEnvironmentVariable("JwtIssuer") ?? builder.Configuration["JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = Environment.GetEnvironmentVariable("JwtAudience") ?? builder.Configuration["JwtAudience"],
                }, out var validatedToken);

                // Verificar si el token ha expirado
                var jwtToken = (JwtSecurityToken)validatedToken;
                if (jwtToken.ValidTo < DateTime.UtcNow)
                    return null;

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}