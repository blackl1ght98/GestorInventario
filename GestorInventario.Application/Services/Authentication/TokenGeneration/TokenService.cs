using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.TokenGeneration;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.TokenGeneration.Generators;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Services.Authentication.TokenGeneration
{
    public class TokenService: ITokenService
    {
     
        private readonly ITokenGenerator _tokenGenerator;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
   
        public TokenService(ITokenGenerator tokenService, IRefreshTokenGenerator refresh)
        {

            _tokenGenerator = tokenService;
            _refreshTokenGenerator = refresh;
     
        }

        public async Task<LoginResponseDto> GenerarToken(Usuario credencialesUsuario)
        {
            // Generar el token principal: dependiendo de la estrategia escogida
            var tokenPrincipal = await _tokenGenerator.GenerateTokenAsync(credencialesUsuario);
            // Generar el token de refresco
            var tokenRefresco = await _refreshTokenGenerator.GenerateTokenAsync(credencialesUsuario);
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
