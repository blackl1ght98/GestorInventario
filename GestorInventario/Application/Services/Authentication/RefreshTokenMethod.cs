
using GestorInventario.Application.Services.Authentication.Strategies;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Common;
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

namespace GestorInventario.Application.Services.Authentication
{
    public class RefreshTokenMethod : IRefreshTokenMethod
    {
        private readonly GestorInventarioContext _context;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cache;
        private readonly ILogger<RefreshTokenMethod> _logger;
        private readonly ITokenStrategyFactory _tokenStrategyFactory;

        public RefreshTokenMethod(
            GestorInventarioContext context,
            IConfiguration configuration,
            ICacheService cache,
            ILogger<RefreshTokenMethod> logger,
            ITokenStrategyFactory tokenStrategyFactory)
        {
            _context = context;
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
            _tokenStrategyFactory = tokenStrategyFactory;
        }

        public async Task<string> GenerarTokenRefresco(Usuario credencialesUsuario)
        {
            var usuarioDB = await _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);

            if (usuarioDB == null)
            {
                throw new ArgumentException("El usuario no existe en la base de datos.");
            }

            var strategy = (BaseTokenStrategy)_tokenStrategyFactory.CreateStrategy();
            var claims = strategy.CrearClaims(credencialesUsuario);

            string authMode = _configuration["AuthMode"] ?? "Symmetric";
            SigningCredentials signingCredentials;

            switch (authMode)
            {
                case "AsymmetricFixed":
                    var privateKeyFixed = Environment.GetEnvironmentVariable("PrivateKey")
                                       ?? _configuration["JWT:PrivateKey"];

                    if (string.IsNullOrEmpty(privateKeyFixed))
                        throw new InvalidOperationException("La clave privada no está configurada.");

                  
                    using (var rsaFixed = RSA.Create())
                    {
                        rsaFixed.FromXmlString(privateKeyFixed);
                        var rsaSecurityKeyFixed = new RsaSecurityKey(rsaFixed.ExportParameters(true));
                        signingCredentials = new SigningCredentials(rsaSecurityKeyFixed, SecurityAlgorithms.RsaSha256);
                    }
                    break;

                case "AsymmetricDynamic":

                    string cacheKey = credencialesUsuario.Id.ToString() + "PublicKeyRefresco";

                    // 1. Recuperar la pública
                    string? publicKeyJson = await _cache.GetStringAsync(cacheKey);

                    // 2. Generamos la nueva pareja de claves
                    RSAParameters privateKey;
                    RSAParameters publicKey;

                   
                    using (var rsa = RSA.Create(2048))
                    {
                        privateKey = rsa.ExportParameters(true);
                        publicKey = rsa.ExportParameters(false);

                        // 3. Guardamos la pública
                        var publicKeyJsonToSave = JsonConvert.SerializeObject(publicKey);

                        // Usamos un tiempo de expiración de 30 días
                        await _cache.SetStringAsync(cacheKey, publicKeyJsonToSave, TimeSpan.FromDays(30));
                    }

                    var rsaSecurityKeyDynamic = new RsaSecurityKey(privateKey)
                    {
                        KeyId = credencialesUsuario.Id.ToString()
                    };
                    signingCredentials = new SigningCredentials(rsaSecurityKeyDynamic, SecurityAlgorithms.RsaSha256);
                    break;

                case "Symmetric":
                    var claveJwt = _configuration["ClaveJWT"];
                    if (string.IsNullOrEmpty(claveJwt))
                    {
                        _logger.LogError("La clave JWT no está configurada.");
                        throw new InvalidOperationException("La clave JWT no está configurada.");
                    }

                    var key = Encoding.UTF8.GetBytes(claveJwt);
                    if (key.Length < 32)
                    {
                        _logger.LogError("La clave JWT es demasiado corta para HMAC-SHA256.");
                        throw new InvalidOperationException("La clave JWT debe tener al menos 32 bytes.");
                    }

                    signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
                    break;

                default:
                    throw new NotSupportedException($"Modo de autenticación no soportado: {authMode}");
            }

            var refreshToken = new JwtSecurityToken(
                issuer: _configuration["JwtIssuer"],
                audience: _configuration["JwtAudience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(refreshToken);
        }
    }
}