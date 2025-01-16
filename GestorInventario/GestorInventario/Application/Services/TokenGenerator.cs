using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Polly;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GestorInventario.Application.Services
{
    public class TokenGenerator : ITokenGenerator
    {
        private readonly GestorInventarioContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<TokenGenerator> _logger;
        private readonly IDistributedCache _redis;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        public TokenGenerator(GestorInventarioContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor,
            IMemoryCache memoryCache, ILogger<TokenGenerator> logger, IDistributedCache cache, IConnectionMultiplexer connection)
        {
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _memoryCache = memoryCache;
            _logger = logger;
            _redis = cache;
            _connectionMultiplexer = connection;
        }
        public async Task<DTOLoginResponse> GenerarTokenSimetrico(Usuario credencialesUsuario)
        {
            var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);
            var claims = new List<Claim>()
            {
                 new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                 new Claim(ClaimTypes.Role, credencialesUsuario.IdRolNavigation.Nombre),
                 new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())


            };

            var clave = Environment.GetEnvironmentVariable("ClaveJWT") ?? _configuration["ClaveJWT"];
            var claveKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));
            var signinCredentials = new SigningCredentials(claveKey, SecurityAlgorithms.HmacSha256);
            var securityToken = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JwtIssuer") ?? _configuration["JwtIssuer"],
                audience: Environment.GetEnvironmentVariable("JwtAudience") ?? _configuration["JwtAudience"],
                claims: claims,
                expires: DateTime.Now.AddDays(30),
                signingCredentials: signinCredentials);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);
            return new DTOLoginResponse()
            {
                Id = credencialesUsuario.Id,
                Token = tokenString,
                Rol = credencialesUsuario.IdRolNavigation.Nombre,
            };
        }
        public async Task<DTOLoginResponse> GenerarTokenAsimetricoFijo(Usuario credencialesUsuario)
        {
            var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);

            var claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                    new Claim(ClaimTypes.Role, credencialesUsuario.IdRolNavigation.Nombre),
                    new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())

                };

            var privateKey = Environment.GetEnvironmentVariable("PrivateKey") ?? _configuration["JWT:PrivateKey"];

            // Convierte la clave privada a formato RSA
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKey);

            // Crea las credenciales de firma con la clave privada
            var signinCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
            var securityToken = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JwtIssuer") ?? _configuration["JwtIssuer"],
                audience: _configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JwtAudience"),
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: signinCredentials);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return new DTOLoginResponse()
            {
                Id = credencialesUsuario.Id,
                Token = tokenString,
                Rol = credencialesUsuario.IdRolNavigation.Nombre,
            };
        }



        public async Task<DTOLoginResponse> GenerarTokenAsimetricoDinamico(Usuario credencialesUsuario)
        {
            // Obtenemos el Id de usuario
            var usuarioDB = await _context.Usuarios
                .Include(u => u.IdRolNavigation) // Carga ansiosa
                .FirstOrDefaultAsync(u => u.Id == credencialesUsuario.Id);

            // Creamos las Claims que el usuario tendrá
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                new Claim(ClaimTypes.Role, credencialesUsuario.IdRolNavigation.Nombre),
                new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())
            };

            // Inicializamos RSA
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                // Creación de la clave pública y privada
                var privateKey = rsa.ExportParameters(true);
                var publicKey = rsa.ExportParameters(false);

                // Creación de la clave AES
                var aes = Aes.Create();
                aes.GenerateKey();
                var aesKey = aes.Key;

                // Encriptamos la clave AES usando RSA
                rsa.ImportParameters(publicKey);
                var encryptedAesKey = rsa.Encrypt(aesKey, true);

                // Encriptamos la clave pública con AES
                var publicKeyCifrada = Cifrar(publicKey.Modulus, aesKey);

                // Convertir las claves a base64
                var privateKeyJson = JsonConvert.SerializeObject(privateKey);
                var encryptedAesKeyBase64 = Convert.ToBase64String(encryptedAesKey);
                var publicKeyCifradaBase64 = Convert.ToBase64String(publicKeyCifrada);

                // Guardamos en Redis o en memoria
                bool useRedis = _connectionMultiplexer != null && _connectionMultiplexer.GetDatabase().Ping().Milliseconds >= 0;
                if (useRedis)
                {
                    await _redis.SetStringAsync(credencialesUsuario.Id.ToString() + "PrivateKey", privateKeyJson);
                    await _redis.SetStringAsync(credencialesUsuario.Id.ToString() + "EncryptedAesKey", encryptedAesKeyBase64);
                    await _redis.SetStringAsync(credencialesUsuario.Id.ToString() + "PublicKey", publicKeyCifradaBase64);
                }
                else
                {
                    _memoryCache.Set(credencialesUsuario.Id.ToString() + "PrivateKey", privateKeyJson);
                    _memoryCache.Set(credencialesUsuario.Id.ToString() + "EncryptedAesKey", encryptedAesKeyBase64);
                    _memoryCache.Set(credencialesUsuario.Id.ToString() + "PublicKey", publicKeyCifradaBase64);
                }

                // Crea la clave de firma con el `kid`
                var rsaSecurityKey = new RsaSecurityKey(privateKey)
                {
                    KeyId = credencialesUsuario.Id.ToString() // Asignamos el `kid` con el ID del usuario
                };

                var signinCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

                // Crea el token
                var securityToken = new JwtSecurityToken(
                    issuer: Environment.GetEnvironmentVariable("JwtIssuer") ?? _configuration["JwtIssuer"],
                    audience: _configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JwtAudience"),
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(10),
                    signingCredentials: signinCredentials);

                var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);

                return new DTOLoginResponse()
                {
                    Id = credencialesUsuario.Id,
                    Token = tokenString,
                    Rol = credencialesUsuario.IdRolNavigation.Nombre,
                };
            }
        }

        public async Task<string> GenerarTokenRefresco(Usuario credencialesUsuario)
        {
            // Obtener usuario de la base de datos
            var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);

            // Definir Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                new Claim(ClaimTypes.Role, credencialesUsuario.IdRolNavigation.Nombre),
                new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())
            };

            // Claves RSA
            string privateKeyJson;
            string publicKeyJson;
            RSAParameters privateKey;
            RSAParameters publicKey;

            bool useRedis = _connectionMultiplexer != null && _connectionMultiplexer.GetDatabase().Ping().Milliseconds >= 0;

            // Intenta recuperar las claves privada y pública almacenadas
            if (useRedis)
            {
                privateKeyJson = await _redis.GetStringAsync(credencialesUsuario.Id.ToString() + "PrivateKeyRefresco");
                publicKeyJson = await _redis.GetStringAsync(credencialesUsuario.Id.ToString() + "PublicKeyRefresco");
            }
            else
            {
                _memoryCache.TryGetValue(credencialesUsuario.Id.ToString() + "PrivateKeyRefresco", out privateKeyJson);
                _memoryCache.TryGetValue(credencialesUsuario.Id.ToString() + "PublicKeyRefresco", out publicKeyJson);
            }

            // Si no existen las claves, generamos una nueva clave pública/privada
            if (string.IsNullOrEmpty(privateKeyJson) || string.IsNullOrEmpty(publicKeyJson))
            {
                using (var rsa = new RSACryptoServiceProvider(2048))
                {
                    privateKey = rsa.ExportParameters(true);
                    publicKey = rsa.ExportParameters(false);

                    privateKeyJson = JsonConvert.SerializeObject(privateKey);
                    publicKeyJson = JsonConvert.SerializeObject(publicKey);

                    // Almacenar las claves (privada y pública) en Redis o en memoria
                    if (useRedis)
                    {
                        await _redis.SetStringAsync(credencialesUsuario.Id.ToString() + "PrivateKeyRefresco", privateKeyJson);
                        await _redis.SetStringAsync(credencialesUsuario.Id.ToString() + "PublicKeyRefresco", publicKeyJson);
                    }
                    else
                    {
                        _memoryCache.Set(credencialesUsuario.Id.ToString() + "PrivateKeyRefresco", privateKeyJson);
                        _memoryCache.Set(credencialesUsuario.Id.ToString() + "PublicKeyRefresco", publicKeyJson);
                    }
                }
            }
            else
            {
                // Convertir JSON de claves privada y pública en objetos RSAParameters
                privateKey = JsonConvert.DeserializeObject<RSAParameters>(privateKeyJson);
                publicKey = JsonConvert.DeserializeObject<RSAParameters>(publicKeyJson);
            }

            // Crear la clave de firma RSA utilizando la clave privada
            var rsaSecurityKey = new RsaSecurityKey(privateKey)
            {
                KeyId = credencialesUsuario.Id.ToString() // Asigna un "kid" único
            };

            var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

            // Generar el token
            var refreshToken = new JwtSecurityToken(
                issuer: _configuration["JwtIssuer"],
                audience: _configuration["JwtAudience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7), // Expira en 7 días
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(refreshToken);
        }



        // Método para cifrar
        public byte[] Cifrar(byte[] data, byte[] aesKey)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = aesKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.GenerateIV();
                    using (var encryptor = aes.CreateEncryptor())
                    {
                        var cipherText = encryptor.TransformFinalBlock(data, 0, data.Length);
                        return aes.IV.Concat(cipherText).ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Error al cifrar", ex);
                throw;
            }
        }

        // Método para descifrar la clave AES utilizando la clave privada
        public byte[] Descifrar(byte[] encryptedData, RSAParameters privateKeyParams)
        {
            try
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(privateKeyParams);
                    return rsa.Decrypt(encryptedData, true);
                }
            }
            catch (Exception ex)
            {
                HandleDecryptionError(ex);
                return new byte[0];
            }
        }



        // Método para descifrar datos utilizando la clave AES
        public byte[] Descifrar(byte[] data, byte[] aesKey)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = aesKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    var iv = data.Take(aes.BlockSize / 8).ToArray();
                    var cipherText = data.Skip(aes.BlockSize / 8).ToArray();
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleDecryptionError(ex);
                return new byte[0];
            }
        }

        // Método para manejar errores de descifrado
        public void HandleDecryptionError(Exception ex)
        {
            var collectioncookies = _httpContextAccessor.HttpContext?.Request.Cookies;
            foreach (var cookie in collectioncookies!)
            {
                _httpContextAccessor.HttpContext?.Response.Cookies.Delete(cookie.Key);
            }
            if (_httpContextAccessor.HttpContext?.Request.Path != "/Auth/Login")
            {
                _httpContextAccessor.HttpContext?.Response.Redirect("/Auth/Login");
            }
            _logger.LogCritical("Error al descifrar", ex);
        }






    }
}
