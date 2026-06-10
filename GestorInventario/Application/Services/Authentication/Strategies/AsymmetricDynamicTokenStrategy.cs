using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies
{
    /// <summary>
    /// Estrategia de generación de tokens JWT utilizando claves RSA asimétricas dinámicas por usuario.
    ///
    /// Modelo de Seguridad:
    /// 1. Generación efímera: Se crea un nuevo par de claves RSA (2048 bits) en cada proceso de login.
    /// 2. Aislamiento de secretos: La clave privada NUNCA se almacena en disco ni en caché.
    ///    Reside únicamente en la RAM durante el tiempo necesario para firmar el token y luego es descartada.
    /// 3. Validación descentralizada: Solo la clave pública se almacena en Redis/MemoryCache.
    ///    Esto permite que el middleware valide la firma del token sin riesgo de comprometer la capacidad de firma del servidor.
    /// 4. Mitigación de riesgos: En caso de un volcado (dump) completo de la caché de Redis, el atacante
    ///    solo obtendrá claves públicas, las cuales son inútiles para falsificar tokens.
    /// </summary>
    public class AsymmetricDynamicTokenStrategy : BaseTokenStrategy
    {
        private readonly ICacheService _cache;
        private readonly ILogger<AsymmetricDynamicTokenStrategy> _logger;
        private readonly IEncryptionService _encryptionService;

        public AsymmetricDynamicTokenStrategy(
            IConfiguration configuration,
            ILogger<AsymmetricDynamicTokenStrategy> logger,
            GestorInventarioContext context,
            ICacheService cache,
            IEncryptionService encryptionService)
            : base(configuration, context)
        {
            
            _logger = logger;
            _cache = cache;
            _encryptionService = encryptionService;
        }

        public override async Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario)
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

            var claims = CrearClaims(credencialesUsuario);

            // 1. Generar un par de claves RSA efímero para esta sesión
            using var rsa = RSA.Create(2048);
            var privateKey = rsa.ExportParameters(true); 
            var publicKey = rsa.ExportParameters(false);   

            // 2. Preparar la clave pública para el almacenamiento en caché
            string publicKeyCacheKey = $"{credencialesUsuario.Id}PublicKey";
            var publicKeyJson = JsonConvert.SerializeObject(publicKey);

            await _cache.SetStringAsync(publicKeyCacheKey, publicKeyJson);

            // 4. Firmar el JWT utilizando la clave privada en memoria
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