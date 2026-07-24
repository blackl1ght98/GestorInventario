using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication;
using GestorInventario.Interfaces.Application.Services.Common;
using GestorInventario.Shared.DTOS.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication.Strategies.AccessToken
{
    public class AsymmetricDynamicTokenStrategy : BaseTokenStrategy
    {
        private readonly ICacheService _cache;
        private readonly ILogger<AsymmetricDynamicTokenStrategy> _logger;

        public AsymmetricDynamicTokenStrategy(
            IConfiguration configuration,
            ITokenClaimsBuilder claimsBuilder,
            ICacheService cache,
            ILogger<AsymmetricDynamicTokenStrategy> logger)
            : base(configuration, claimsBuilder)
        {
            _cache = cache;
            _logger = logger;
        }

        public override async Task<LoginResponseDto> GenerateTokenAsync(Usuario usuario)
        {
            if (usuario.IdRolNavigation is null)
            {
                _logger.LogError("El usuario {UserId} no tiene un rol asignado.", usuario.Id);
                throw new InvalidOperationException("El usuario no tiene un rol asignado.");
            }

            using var rsa = RSA.Create(2048);
            var privateKey = rsa.ExportParameters(true);
            var publicKey = rsa.ExportParameters(false);

            await _cache.SetStringAsync(
                $"{usuario.Id}PublicKey",
                JsonConvert.SerializeObject(publicKey));

            var credentials = new SigningCredentials(
                new RsaSecurityKey(privateKey) { KeyId = usuario.Id.ToString() },
                SecurityAlgorithms.RsaSha256);
            var minutos = _claimsBuilder.ObtenerDuracionAccessTokenMinutos();
            var token = new JwtSecurityToken(
                issuer: _claimsBuilder.ObtenerIssuer(),
                audience: _claimsBuilder.ObtenerAudience(),
                claims: _claimsBuilder.CrearClaims(usuario),
                expires: DateTime.UtcNow.AddMinutes(minutos),
                signingCredentials: credentials);

            return new LoginResponseDto
            {
                Id = usuario.Id,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Rol = usuario.IdRolNavigation.Nombre
            };
        }

    }
}