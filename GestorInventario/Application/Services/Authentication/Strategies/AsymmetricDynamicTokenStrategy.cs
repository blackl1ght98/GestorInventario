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
    public class AsymmetricDynamicTokenStrategy : ITokenStrategy
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

            // 1. Generar par de claves RSA (2048 bits, seguro)
            using var rsa = RSA.Create(2048);
            var privateKey = rsa.ExportParameters(true);
            var publicKey = rsa.ExportParameters(false);

            // 2. Generar clave AES simétrica por sesión
            using var aes = Aes.Create();
            aes.GenerateKey();
            var aesKey = aes.Key;

            // 3. Cifrar la clave AES con la clave pública RSA (OAEP-SHA256)
            rsa.ImportParameters(publicKey);
            var encryptedAesKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);

            // 4. Guardar en Redis o MemoryCache (solo privada + AES cifrada)
            var privateKeyJson = JsonConvert.SerializeObject(privateKey);
            var encryptedAesKeyBase64 = Convert.ToBase64String(encryptedAesKey);

            bool useRedis = _connectionMultiplexer != null && _connectionMultiplexer.GetDatabase().Ping().Milliseconds >= 0;
            if (useRedis)
            {
                await _redis.SetStringAsync(credencialesUsuario.Id.ToString() + "PrivateKey", privateKeyJson);
                await _redis.SetStringAsync(credencialesUsuario.Id.ToString() + "EncryptedAesKey", encryptedAesKeyBase64);

            }
            else
            {
                _memoryCache.Set(credencialesUsuario.Id.ToString() + "PrivateKey", privateKeyJson);
                _memoryCache.Set(credencialesUsuario.Id.ToString() + "EncryptedAesKey", encryptedAesKeyBase64);
            }

            // 5. Firmar JWT con la clave privada RSA
            var rsaSecurityKey = new RsaSecurityKey(privateKey)
            {
                KeyId = credencialesUsuario.Id.ToString()
            };

            var signinCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

            var securityToken = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JwtIssuer") ?? _configuration["JwtIssuer"],
                audience: _configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JwtAudience"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(10),
                signingCredentials: signinCredentials);

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
