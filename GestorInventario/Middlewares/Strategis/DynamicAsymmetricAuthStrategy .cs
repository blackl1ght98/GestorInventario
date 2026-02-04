using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GestorInventario.Middlewares.Strategis
{
    /// <summary>
    /// Estrategia de autenticación dinámica basada en JWT asimétrico por usuario.
    /// 
    /// Características principales:
    /// - Claves RSA privadas y AES cifradas almacenadas por usuario (kid)
    /// - Soporte para refresh token con rotación
    /// - Almacenamiento en Redis (preferido) o MemoryCache
    /// - Validación estricta de firma, issuer, audience y lifetime
    /// - Regeneración automática de access token cuando expira (usando refresh)
    /// - Redirección a login si ambos tokens fallan
    /// 
    /// Flujo general:
    /// 1. Intenta validar access token
    /// 2. Si está expirado → intenta refresh
    /// 3. Si refresh también falla → borra cookies y redirige a login
    /// </summary>
    public class DynamicAsymmetricAuthStrategy : IAuthProcessingStrategy
    {
        public async Task ProcessAuthentication(HttpContext context, Func<Task> next)
        {
            try
            {
                // 1º Llamada la los serivicios necesarios 
                var token = context.Request.Cookies["auth"];
                var refreshToken = context.Request.Cookies["refreshToken"];
                var httpContextAccessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();
                var tokenService = context.RequestServices.GetRequiredService<ITokenGenerator>();
                var encryptionService = context.RequestServices.GetRequiredService<IEncryptionService>();
                var userService = context.RequestServices.GetRequiredService<IAdminRepository>();
                var refreshTokenMethod = context.RequestServices.GetRequiredService<IRefreshTokenMethod>();
                var redis = context.RequestServices.GetService<IDistributedCache>();
                var memoryCache = context.RequestServices.GetService<IMemoryCache>();
                var connectionMultiplexer = context.RequestServices.GetService<IConnectionMultiplexer>();
                var configuration = context.RequestServices.GetService<IConfiguration>();
                bool useRedis = connectionMultiplexer?.IsConnected ?? false;

                // 2º Validacion de datos, dependiendo de si las validaciones se cumplen o no se ejecutara el metodo oportuno
                if (!string.IsNullOrEmpty(token))
                {
                    // Metodo encargado de validar el token
                    var (jwtToken, principal) = await ValidateToken(token, configuration, tokenService, redis, memoryCache, encryptionService, useRedis);
                    if (jwtToken != null && principal != null)
                    {
                       
                        context.User = principal;
                        var logger = log4net.LogManager.GetLogger(typeof(DynamicAsymmetricAuthStrategy));
                        logger.Info($"Claims establecidos en HttpContext.User: {string.Join(", ", principal.Claims.Select(c => $"{c.Type}: {c.Value}"))}");
                    }
                    //Validacion del metodo de refresco
                    else if (!string.IsNullOrEmpty(refreshToken))
                    {
                        //Manejador en caso de que expire el token de acceso "auth"
                        await HandleExpiredToken(context, refreshToken, configuration, tokenService, userService, redis, memoryCache, refreshTokenMethod, useRedis);

                    }
                }
                else if (!string.IsNullOrEmpty(refreshToken))
                {
                    await HandleExpiredToken(context, refreshToken, configuration, tokenService, userService, redis, memoryCache, refreshTokenMethod, useRedis);

                }
            }
            catch (Exception ex)
            {
                var logger = log4net.LogManager.GetLogger(typeof(DynamicAsymmetricAuthStrategy));
                logger.Error("Error en el middleware de autenticación", ex);
            }

            await next();
        }

        //Metodo encargado de validar el token
        private static async Task<(JwtSecurityToken?, ClaimsPrincipal?)> ValidateToken(
        string token,
        IConfiguration configuration,
        ITokenGenerator tokenService,
        IDistributedCache? redis,
        IMemoryCache? memoryCache,
        IEncryptionService encryptionService,
        bool useRedis)
        {
            //Manejador del token y logs
            var handler = new JwtSecurityTokenHandler();
            var logger = log4net.LogManager.GetLogger(typeof(DynamicAsymmetricAuthStrategy));

            try
            {
                //1º Leemos el token
                var jwtToken = handler.ReadJwtToken(token);
                //2º Validamos si el token a caducado
                if (jwtToken.ValidTo < DateTime.UtcNow)
                {
                    logger.Warn("Token ha expirado.");
                    return (null, null);
                }
                //3º Obtenemos el identificador del usuario que esta en el token
                string kid = jwtToken.Header["kid"]?.ToString();
                if (string.IsNullOrEmpty(kid))
                {
                    logger.Warn("El token no contiene un 'kid'.");
                    return (null, null);
                }

                // 4º Recuperamos solo privada y AES cifrada
                var (privateKeyParams, encryptedAesKeyBase64) = await RetrieveKeys(kid, redis, memoryCache, useRedis);
                if (privateKeyParams == null || string.IsNullOrEmpty(encryptedAesKeyBase64))
                {
                    logger.Warn($"No se pudieron recuperar las claves para el usuario con kid: {kid}");
                    return (null, null);
                }

                // 5º Descifrar AES con la clave  privada RSA
                var privateKeyBytes = ToPrivateKeyBytes(privateKeyParams.Value);

                var aesKey = encryptionService.Descifrar(
                    Convert.FromBase64String(encryptedAesKeyBase64),
                    privateKeyBytes
                );

                // 6º Crear clave de firma RSA (usamos la privada para validar)
                var rsaSecurityKey = new RsaSecurityKey(privateKeyParams.Value);
                // 7º Validamos
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = rsaSecurityKey,
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtAudience"],
                    ValidateLifetime = true
                };
                // 8º Validación REAL del token + obtención del ClaimsPrincipal confiable
               
                var principal = handler.ValidateToken(token, validationParameters, out _);
                
                var tokenPayload = jwtToken.Claims.Select(c => $"{c.Type}: {c.Value}");
                logger.Info($"Claims validados en el token: {string.Join(", ", tokenPayload)}");
                // 9. Si llegamos aquí significa que:
                //    - La firma es válida (usando la clave recuperada con el kid)
                //    - Issuer, Audience y lifetime son correctos
                //    - El token NO ha sido manipulado
                // → Devolvemos tanto el token parseado (JwtSecurityToken) como 
                //   el ClaimsPrincipal ya validado y confiable
                return (jwtToken, principal);
            }
            catch (Exception ex)
            {
                logger.Error($"Error al validar el token: {ex.Message}", ex);
                return (null, null);
            }
        }
        private static byte[] ToPrivateKeyBytes(RSAParameters parameters)
        {
            using var rsa = RSA.Create();
            rsa.ImportParameters(parameters);
            return rsa.ExportRSAPrivateKey(); 
        }
        //Metodo que se encarga de regenerar el token de acceso en caso de expiracion
        private static async Task HandleExpiredToken(HttpContext context, string refreshToken, IConfiguration configuration, ITokenGenerator tokenService, 
        IAdminRepository userService, IDistributedCache? redis,IMemoryCache? memoryCache, IRefreshTokenMethod refreshTokenMethod, bool useRedis)
        {
            //1º Llamamos al metodo encargado de validar el token de refresco
            var refreshTokenValid = await ValidateRefreshToken(refreshToken, configuration, redis, memoryCache, useRedis);
            var logger = log4net.LogManager.GetLogger(typeof(DynamicAsymmetricAuthStrategy));
      
            if (!refreshTokenValid)
            {
                logger.Warn("Refresh token no válido o expirado.");
                RedirectToLogin(context, context.Request.Cookies);
                return;
            }
            //2º Llamamos al manejador de tokens de .Net
            var handler = new JwtSecurityTokenHandler();
            //3º Leemos el token
            var token = handler.ReadJwtToken(refreshToken);
            //4º Obtenemos el Id de usuario del token
            var userId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            //5º Validamos
            if (string.IsNullOrEmpty(userId))
            {
                logger.Warn("No se encontró userId en el refresh token.");
                RedirectToLogin(context, context.Request.Cookies);
                return;
            }
            //6º Aseguramos que exista el usuario en base de datos
            var (user, mensaje) = await userService.ObtenerPorId(int.Parse(userId));
            if (user == null)
            {
                logger.Warn($"Usuario con ID {userId} no encontrado.");
                RedirectToLogin(context, context.Request.Cookies);
                return;
            }
            //7º Llamamos a los metodos para regenerar ambos token y creamos las cookies
            var newAccessToken = await tokenService.GenerateTokenAsync(user);
            var newRefreshToken = await refreshTokenMethod.GenerarTokenRefresco(user);


            context.Response.Cookies.Append("auth", newAccessToken.Token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Domain = "localhost",
                Secure = true,
                Expires = DateTime.UtcNow.AddMinutes(10)
            });

            context.Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Domain = "localhost",
                Secure = true,
                Expires = DateTime.UtcNow.AddHours(24)
            });

            logger.Info($"Nuevos tokens generados para el usuario {userId}.");
        }
        //Metodo encargado de recuperar las claves de la memoria
        public static async Task<(RSAParameters?, string?)> RetrieveKeys(string kid, IDistributedCache? redis, IMemoryCache? memoryCache, bool useRedis)
        {
            string? privateKeyJson = null;
            string? encryptedAesKeyBase64 = null;
            //1º Validamos si usamos redis si no lo usamos las extraemos de la memoria
            if (useRedis && redis != null)
            {
                privateKeyJson = await redis.GetStringAsync($"{kid}PrivateKey");
                encryptedAesKeyBase64 = await redis.GetStringAsync($"{kid}EncryptedAesKey");
            }
            else if (memoryCache != null)
            {
                memoryCache.TryGetValue($"{kid}PrivateKey", out privateKeyJson);
                memoryCache.TryGetValue($"{kid}EncryptedAesKey", out encryptedAesKeyBase64);
            }
            //2º Validamos la clave privada y si no es null la deserializamos
            RSAParameters? privateKey = null;
            if (!string.IsNullOrEmpty(privateKeyJson))
            {
                privateKey = JsonConvert.DeserializeObject<RSAParameters>(privateKeyJson);
            }
            //3º devolvemos la clave privada y la clave aes encriptada

            return (privateKey, encryptedAesKeyBase64);
        }
        //Metodo encargado de validar el token de refresco
        private static async Task<bool> ValidateRefreshToken(
          string refreshToken,
          IConfiguration configuration,
          IDistributedCache? redis,
          IMemoryCache? memoryCache,
          bool useRedis)
        {
            try
            {
                //1º Llamamos al manejador de tokens de .Net
                var handler = new JwtSecurityTokenHandler();
                //2º Leemos el token de refresco
                var jwtToken = handler.ReadJwtToken(refreshToken);
                //3º Validamos que no haya cumplido
                if (jwtToken.ValidTo < DateTime.UtcNow)
                    return false;
                //4º Recuperamos el Id de usuario del token
                string kid = jwtToken.Header.Kid;
                if (string.IsNullOrEmpty(kid))
                    return false;

                //5º Recuperamos y validamos la clave publica
                string? publicKeyJson = null;

                if (useRedis && redis != null)
                    publicKeyJson = await redis.GetStringAsync($"{kid}PublicKeyRefresco");
                else if (memoryCache != null)
                    memoryCache.TryGetValue($"{kid}PublicKeyRefresco", out publicKeyJson);

                if (string.IsNullOrEmpty(publicKeyJson))
                    return false;

                //6º Si la validacion ha ido bien deserializamos la clave
                var publicKeyParams =
                    JsonConvert.DeserializeObject<RSAParameters>(publicKeyJson);
                //7º Creamos una clave RSA
                var rsa = RSA.Create();
                //8º La guardamos
                rsa.ImportParameters(publicKeyParams);
                //9º A esa clave le asignamos el id de usuario
                var rsaSecurityKey = new RsaSecurityKey(rsa)
                {
                    KeyId = kid
                };
                //10º Validamos la clave generada
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = rsaSecurityKey,
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtAudience"],
                    ValidateLifetime = true
                };
                //11º Validamos el token 
                handler.ValidateToken(refreshToken, validationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }


        private static void RedirectToLogin(HttpContext context, IRequestCookieCollection collectionCookies)
        {
            foreach (var cookie in collectionCookies)
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