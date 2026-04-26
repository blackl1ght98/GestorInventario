using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Application.DTOs.User;

namespace GestorInventario.Application.Services
{
    public class TokenService: ITokenService
    {
     
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IRefreshTokenMethod _refreshTokenMethod;
      
        public TokenService(ITokenGenerator tokenService, IRefreshTokenMethod refresh)
        {

            _tokenGenerator = tokenService;
            _refreshTokenMethod = refresh;
          
        }
      
        public async Task<LoginResponseDto> GenerarToken(Usuario credencialesUsuario)
        {
            // Generar el token principal: dependiendo de la estrategia escogida
            var tokenPrincipal = await _tokenGenerator.GenerateTokenAsync(credencialesUsuario);
            // Generar el token de refresco
            var tokenRefresco = await _refreshTokenMethod.GenerarTokenRefresco(credencialesUsuario);
            // Devolver ambos tokens en la respuesta
            return new LoginResponseDto
            {
                Id = tokenPrincipal.Id,
                Token = tokenPrincipal.Token,
                Rol = tokenPrincipal.Rol,
                RefreshToken = tokenRefresco
            };
        }

    }
}
