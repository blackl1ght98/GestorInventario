using GestorInventario.Application.DTOs.User;
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
using System.Text;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class AsymmetricDynamicTokenStrategy : ITokenStrategy
    {
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _redis;
        private readonly IMemoryCache _memoryCache;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<TokenGenerator> _logger;
        private readonly GestorInventarioContext _context;

        // Clave maestra estática: se genera SOLO UNA VEZ cuando la clase se carga
        public static readonly byte[] MasterKey = GenerateMasterKey();

        private static byte[] GenerateMasterKey()
        {
            var key = new byte[32]; // AES-256 requiere exactamente 32 bytes
            RandomNumberGenerator.Fill(key);
            return key;
        }

        public AsymmetricDynamicTokenStrategy(IConfiguration configuration, IDistributedCache redis,
            IMemoryCache memoryCache, IConnectionMultiplexer connectionMultiplexer,
            ILogger<TokenGenerator> logger, GestorInventarioContext context)
        {
            _configuration = configuration;
            _redis = redis;
            _memoryCache = memoryCache;
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
            _context = context;
        }

        public async Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            var usuarioDB = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .FirstOrDefaultAsync(u => u.Id == credencialesUsuario.Id);

            if (usuarioDB == null)
            {
                _logger.LogError($"Usuario con ID {credencialesUsuario.Id} no encontrado en la base de datos.");
                throw new ArgumentException("El usuario no existe en la base de datos.");
            }

            if (usuarioDB.IdRolNavigation == null)
            {
                _logger.LogError($"El usuario con ID {credencialesUsuario.Id} no tiene un rol asignado.");
                throw new InvalidOperationException("El usuario no tiene un rol asignado.");
            }

            _logger.LogInformation($"Rol del usuario {credencialesUsuario.Id}: {usuarioDB.IdRolNavigation.Nombre}");

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                new Claim(ClaimTypes.Role, usuarioDB.IdRolNavigation.Nombre),
                new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())
            };

            // 1. Generar par de claves RSA
            using var rsa = RSA.Create(2048);
            var privateKey = rsa.ExportParameters(true);
            var publicKey = rsa.ExportParameters(false);

            // 2. AES para el token
            using var aes = Aes.Create();
            aes.GenerateKey();
            var aesKey = aes.Key;

            // 3. Cifrar AES con pública RSA
            rsa.ImportParameters(publicKey);
            var encryptedAesKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);
            var encryptedAesKeyBase64 = Convert.ToBase64String(encryptedAesKey);

            // 4. Serializar la clave privada RSA a JSON
            var privateKeyJson = JsonConvert.SerializeObject(privateKey);

            // 5. CIFRAR la clave privada con la clave maestra estática
            string encryptedPrivateBase64 = EncryptPrivateKey(privateKeyJson);

            // 6. Guardar en caché: la privada CIFRADA + la AES cifrada
            string privateKeyCacheKey = $"{credencialesUsuario.Id}PrivateKey";
            string aesKeyCacheKey = $"{credencialesUsuario.Id}EncryptedAesKey";

            bool useRedis = _connectionMultiplexer != null && _connectionMultiplexer.IsConnected;

            if (useRedis)
            {
                await _redis.SetStringAsync(privateKeyCacheKey, encryptedPrivateBase64);
                await _redis.SetStringAsync(aesKeyCacheKey, encryptedAesKeyBase64);
            }
            else
            {
                _memoryCache.Set(privateKeyCacheKey, encryptedPrivateBase64);
                _memoryCache.Set(aesKeyCacheKey, encryptedAesKeyBase64);
            }

            // 7. Firmar JWT con la clave privada original (en memoria)
            var rsaSecurityKey = new RsaSecurityKey(privateKey)
            {
                KeyId = credencialesUsuario.Id.ToString()
            };

            var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

            var securityToken = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JwtIssuer") ?? _configuration["JwtIssuer"],
                audience: _configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JwtAudience"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: signingCredentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return new LoginResponseDto()
            {
                Id = credencialesUsuario.Id,
                Token = tokenString,
                Rol = usuarioDB.IdRolNavigation.Nombre,
            };
        }

        // Método helper para cifrar la clave privada
        private string EncryptPrivateKey(string privateKeyJson)
        {
            using var aesMaster = Aes.Create();
            aesMaster.Key = MasterKey;              // ← Usamos la clave estática
            aesMaster.GenerateIV();                 // IV aleatorio cada vez

            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, aesMaster.CreateEncryptor(), CryptoStreamMode.Write);

            byte[] privateBytes = Encoding.UTF8.GetBytes(privateKeyJson);
            cs.Write(privateBytes, 0, privateBytes.Length);
            cs.FlushFinalBlock();

            // Guardamos IV + ciphertext juntos
            byte[] ivAndCipher = aesMaster.IV.Concat(ms.ToArray()).ToArray();
            return Convert.ToBase64String(ivAndCipher);
        }
    }
}