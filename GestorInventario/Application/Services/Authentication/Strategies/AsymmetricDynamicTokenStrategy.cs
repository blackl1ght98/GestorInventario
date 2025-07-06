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
            var usuarioDB = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .ThenInclude(r => r.RolePermisos)
                .ThenInclude(rp => rp.Permiso)
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

            // Log para verificar el rol
            _logger.LogInformation($"Rol del usuario {credencialesUsuario.Id}: {usuarioDB.IdRolNavigation.Nombre}");

            // Verificar los permisos asociados al rol
            var permisos = usuarioDB.IdRolNavigation.RolePermisos?.Select(rp => rp.Permiso?.Nombre) ?? Enumerable.Empty<string>();
            var permisosList = permisos.ToList();

            // Log para verificar los permisos
            _logger.LogInformation($"Permisos encontrados para el usuario {credencialesUsuario.Id}: {string.Join(", ", permisosList)}");

            // Creamos las Claims que el usuario tendrá
            var claims = new List<Claim>()
            {
                 new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                new Claim(ClaimTypes.Role, usuarioDB.IdRolNavigation.Nombre),
                new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())
            };

            foreach (var permiso in permisosList)
            {
                if (!string.IsNullOrEmpty(permiso))
                {
                    claims.Add(new Claim("permiso", permiso, ClaimValueTypes.String, issuer: "GestorInvetarioEmisor"));
                    _logger.LogInformation($"Claim añadido en refresh token: permiso={permiso}");
                }
                else
                {
                    _logger.LogWarning($"Permiso vacío encontrado para el usuario {credencialesUsuario.Id}.");
                }
            }

            // Resto del código sin cambios...
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                var privateKey = rsa.ExportParameters(true);
                var publicKey = rsa.ExportParameters(false);

                var aes = Aes.Create();
                aes.GenerateKey();
                var aesKey = aes.Key;

                rsa.ImportParameters(publicKey);
                var encryptedAesKey = rsa.Encrypt(aesKey, true);

                var publicKeyCifrada = Cifrar(publicKey.Modulus, aesKey);

                var privateKeyJson = JsonConvert.SerializeObject(privateKey);
                var encryptedAesKeyBase64 = Convert.ToBase64String(encryptedAesKey);
                var publicKeyCifradaBase64 = Convert.ToBase64String(publicKeyCifrada);

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
                // Log del token completo para inspección
                _logger.LogInformation($"Token generado: {tokenString}");
                var tokenPayload = new JwtSecurityTokenHandler().ReadJwtToken(tokenString).Claims
                    .Select(c => $"{c.Type}: {c.Value}");
                _logger.LogInformation($"Claims en el token: {string.Join(", ", tokenPayload)}");
                return new DTOLoginResponse()
                {
                    Id = credencialesUsuario.Id,
                    Token = tokenString,
                    Rol = usuarioDB.IdRolNavigation.Nombre,
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
