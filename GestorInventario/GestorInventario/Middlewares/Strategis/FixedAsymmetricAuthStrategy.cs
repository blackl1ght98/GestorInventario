using GestorInventario.Application.Services;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
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
                var token = context.Request.Cookies["auth"];
                var refreshToken = context.Request.Cookies["refreshToken"];
                var tokenService = context.RequestServices.GetRequiredService<ITokenGenerator>();
                var userService = context.RequestServices.GetRequiredService<IAdminRepository>();
                var refreshTokenMethod = context.RequestServices.GetRequiredService<IRefreshTokenMethod>();
                IDistributedCache? redis = null;
                IMemoryCache? memoryCache = null;
                bool useRedis = false;

                if (!string.IsNullOrEmpty(token))
                {
                    var jwtToken = await ValidateToken(token, builder, tokenService, redis, memoryCache, useRedis);
                    if (jwtToken == null && !string.IsNullOrEmpty(refreshToken))
                    {
                        await HandleExpiredToken(context, refreshToken, builder, tokenService, userService, redis, memoryCache, refreshTokenMethod, useRedis);
                    }
                }
                else if (!string.IsNullOrEmpty(refreshToken))
                {
                    await HandleExpiredToken(context, refreshToken, builder, tokenService, userService, redis, memoryCache, refreshTokenMethod, useRedis);
                }
            }
            catch (Exception ex)
            {
                var logger = log4net.LogManager.GetLogger(typeof(Program));
                logger.Error("Error en el middleware de autenticación", ex);
            }

            await next();
        }

        private static async Task<JwtSecurityToken?> ValidateToken(string token, WebApplicationBuilder builder, ITokenGenerator tokenService, IDistributedCache? redis, IMemoryCache? memoryCache, bool useRedis)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                var jwtToken = handler.ReadJwtToken(token);
                if (jwtToken.ValidTo < DateTime.UtcNow)
                    return null;

                var publicKey = builder.Configuration["JWT:PublicKey"];
                if (string.IsNullOrEmpty(publicKey))
                    return null;

                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(publicKey);
                var rsaSecurityKey = new RsaSecurityKey(rsa);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = rsaSecurityKey,
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JwtAudience"]
                };

                handler.ValidateToken(token, validationParameters, out _);
                return jwtToken;
            }
            catch
            {
                return null;
            }
        }

        private static async Task HandleExpiredToken(HttpContext context, string refreshToken, WebApplicationBuilder builder, ITokenGenerator tokenService, IAdminRepository userService, IDistributedCache? redis, IMemoryCache? memoryCache, IRefreshTokenMethod refreshTokenMethod, bool useRedis)
        {
            var refreshTokenValid = await ValidateRefreshToken(refreshToken, builder, redis, memoryCache, useRedis);
            if (!refreshTokenValid)
                return;

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(refreshToken);
            var userId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return;

            var user = await userService.ObtenerPorId(int.Parse(userId));
            if (user == null)
                return;

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

        private static async Task<bool> ValidateRefreshToken(string refreshToken, WebApplicationBuilder builder, IDistributedCache? redis, IMemoryCache? memoryCache, bool useRedis)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(refreshToken);
                if (token.ValidTo < DateTime.UtcNow)
                    return false;

                var publicKey = builder.Configuration["JWT:PublicKey"];
                if (string.IsNullOrEmpty(publicKey))
                    return false;

                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(publicKey);
                var rsaSecurityKey = new RsaSecurityKey(rsa);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = rsaSecurityKey,
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JwtAudience"]
                };

                handler.ValidateToken(refreshToken, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}