using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class AsymmetricDynamicTokenStrategy: ITokenStrategy
    {
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _redis;
        private readonly IMemoryCache _memoryCache;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<TokenGenerator> _logger;
        private readonly GestorInventarioContext _context;
        public AsymmetricDynamicTokenStrategy(IConfiguration configuration, IDistributedCache redis,
            IMemoryCache memoryCache, IConnectionMultiplexer connectionMultiplexer, ILogger<TokenGenerator> logger, GestorInventarioContext context)
        {
            _configuration = configuration;
            _redis = redis;
            _memoryCache = memoryCache;
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
            _context = context;
        }
        public async Task<DTOLoginResponse> GenerateTokenAsync(Usuario credencialesUsuario)
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

    }
}
