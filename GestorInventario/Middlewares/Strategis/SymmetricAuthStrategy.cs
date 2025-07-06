using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.Extensions.Logging;
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
            try
            {
                var secret = Environment.GetEnvironmentVariable("ClaveJWT") ?? builder.Configuration["ClaveJWT"];
                if (string.IsNullOrEmpty(secret))
                {
                   
                    throw new InvalidOperationException("La clave JWT es requerida.");
                }

                // Recuperar cookies y servicios necesarios
                var token = context.Request.Cookies["auth"];
                var refreshToken = context.Request.Cookies["refreshToken"];
                var tokenService = context.RequestServices.GetRequiredService<ITokenGenerator>();
                var userService = context.RequestServices.GetRequiredService<IAdminRepository>();
                var refreshTokenMethod = context.RequestServices.GetRequiredService<IRefreshTokenMethod>();

                // Validar el token principal
                if (!string.IsNullOrEmpty(token))
                {
                    var (jwtToken, principal) = await ValidateToken(token, secret, builder);
                    if (jwtToken != null && principal != null)
                    {
                        // Establecer el ClaimsPrincipal en HttpContext.User
                        context.User = principal;
                       
                    }
                    else if (!string.IsNullOrEmpty(refreshToken))
                    {
                        await HandleExpiredToken(context, refreshToken, secret, builder, tokenService, userService, refreshTokenMethod);
                    }
                }
                else if (!string.IsNullOrEmpty(refreshToken))
                {
                    await HandleExpiredToken(context, refreshToken, secret, builder, tokenService, userService, refreshTokenMethod);
                }
               
            }
            catch (Exception ex)
            {
                
            }

            await next();
        }

        private async Task<(JwtSecurityToken?, ClaimsPrincipal?)> ValidateToken(string token, string secret, WebApplicationBuilder builder)
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
                    ValidIssuer = builder.Configuration["JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JwtAudience"],
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

        private async Task HandleExpiredToken(HttpContext context, string refreshToken, string secret, WebApplicationBuilder builder,
            ITokenGenerator tokenService, IAdminRepository userService, IRefreshTokenMethod refreshTokenMethod)
        {
            var refreshTokenValid = await ValidateRefreshToken(refreshToken, secret, builder);
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

            var user = await userService.ObtenerPorId(userIdParsed);
            if (user == null)
            {
                
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

           
        }

        private async Task<bool> ValidateRefreshToken(string refreshToken, string secret, WebApplicationBuilder builder)
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
                    ValidIssuer = builder.Configuration["JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JwtAudience"],
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