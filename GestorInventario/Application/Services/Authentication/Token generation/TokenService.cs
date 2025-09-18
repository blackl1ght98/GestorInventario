using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Application.DTOs.User;

namespace GestorInventario.Application.Services
{
    public class TokenService
    {
     
        private readonly ITokenGenerator _tokenService;
        private readonly IRefreshTokenMethod _refreshTokenMethod;
        public TokenService(ITokenGenerator tokenService, IRefreshTokenMethod refresh)
        {
            
            _tokenService = tokenService;
            _refreshTokenMethod = refresh;
        }
      
        public async Task<LoginResponseDto> GenerarToken(Usuario credencialesUsuario)
        {
            // Generar el token principal
            var tokenPrincipal = await _tokenService.GenerateTokenAsync(credencialesUsuario);
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
