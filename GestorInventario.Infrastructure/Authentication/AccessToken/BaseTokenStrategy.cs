using System.IdentityModel.Tokens.Jwt;
using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Domain.Models;
using Microsoft.IdentityModel.Tokens;

namespace GestorInventario.Infrastructure.Authentication.AccessToken
{
    public abstract class BaseTokenStrategy : ITokenStrategy
    {
        
        protected readonly IJwtTokenSettings _jwtTokenSettings;

        protected BaseTokenStrategy( IJwtTokenSettings jwtTokenSettings)
        {
           
            _jwtTokenSettings = jwtTokenSettings;
        }

        // Cada estrategia hija solo se encarga de proveer las credenciales de firma
        protected abstract Task<SigningCredentials> GetSigningCredentialsAsync(Usuarios usuario);

        public virtual async Task<LoginResponseDto> GenerateTokenAsync(Usuarios usuario)
        {
            if (usuario.IdRolNavigation is null)
            {
                throw new InvalidOperationException($"El usuario {usuario.Id} no tiene un rol asignado.");
            }

            // Obtenemos las credenciales específicas de la estrategia (Symmetric, Asymmetric, etc.)
            var credentials = await GetSigningCredentialsAsync(usuario);

            var minutos = _jwtTokenSettings.ObtenerDuracionAccessTokenMinutos();
            var token = new JwtSecurityToken(
                issuer: _jwtTokenSettings.ObtenerIssuer(),
                audience: _jwtTokenSettings.ObtenerAudience(),
                claims: _jwtTokenSettings.CrearClaims(usuario),
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
