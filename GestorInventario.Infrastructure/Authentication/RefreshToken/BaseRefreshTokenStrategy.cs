using System.IdentityModel.Tokens.Jwt;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Domain.Models;
using Microsoft.IdentityModel.Tokens;

namespace GestorInventario.Infrastructure.Authentication.RefreshToken
{
    public abstract class BaseRefreshTokenStrategy: IRefreshTokenStrategy
    {
        protected readonly IJwtTokenSettings _jwtTokenSettings;
       
        protected BaseRefreshTokenStrategy(IJwtTokenSettings jwtTokenSettings)
        {
           
            _jwtTokenSettings = jwtTokenSettings;
            
        }
        protected abstract Task<SigningCredentials> GetSigningCredentialsAsync(Usuarios credencialesUsuario);

        public virtual async Task<string> GenerarTokenRefresco(Usuarios credencialesUsuario)
        {
            if (credencialesUsuario.IdRolNavigation is null)
            {
                throw new InvalidOperationException($"El usuario {credencialesUsuario.Id} no tiene un rol asignado.");
            }

            var credentials = await GetSigningCredentialsAsync(credencialesUsuario);

            var horas = _jwtTokenSettings.ObtenerDuracionRefreshTokenHoras();
            var token = new JwtSecurityToken(
                issuer: _jwtTokenSettings.ObtenerIssuer(),
                audience: _jwtTokenSettings.ObtenerAudience(),
                claims: _jwtTokenSettings.CrearClaims(credencialesUsuario),
                expires: DateTime.UtcNow.AddHours(horas),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
