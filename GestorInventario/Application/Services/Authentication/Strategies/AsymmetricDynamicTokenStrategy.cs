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

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    public class AsymmetricDynamicTokenStrategy : BaseTokenStrategy
    {
        private readonly IDistributedCache _redis;
        private readonly IMemoryCache _memoryCache;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<AsymmetricDynamicTokenStrategy> _logger;
        private readonly IEncryptionService _encryptionService;

        public AsymmetricDynamicTokenStrategy(
            IConfiguration configuration,
            IDistributedCache redis,
            IMemoryCache memoryCache,
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<AsymmetricDynamicTokenStrategy> logger,
            GestorInventarioContext context,
            IEncryptionService encryptionService)
            : base(configuration, context)
        {
            _redis = redis;
            _memoryCache = memoryCache;
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        public override async Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            // Obtener usuario de la base de datos
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

            // Usamos el método de la clase base
            var claims = CrearClaims(credencialesUsuario);

            // 1. Generar par de claves RSA
            using var rsa = RSA.Create(2048);
            var privateKey = rsa.ExportParameters(true);
            var publicKey = rsa.ExportParameters(false);

            // 2. AES para el token
            using var aes = Aes.Create();
            aes.GenerateKey();
            var aesKey = aes.Key;

            // 3. Cifrar AES con la clave pública RSA
            rsa.ImportParameters(publicKey);
            var encryptedAesKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);
            var encryptedAesKeyBase64 = Convert.ToBase64String(encryptedAesKey);

            // 4. Serializar la clave privada RSA a JSON
            var privateKeyJson = JsonConvert.SerializeObject(privateKey);

            // 5. CIFRAR la clave privada con la clave maestra estática
            string encryptedPrivateBase64 = _encryptionService.EncryptPrivateKey(privateKeyJson);

            // 6. Guardar en caché
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
                issuer: ObtenerIssuer(),
                audience: ObtenerAudience(),
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
    }
}