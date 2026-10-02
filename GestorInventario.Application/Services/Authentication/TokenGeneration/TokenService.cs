using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.TokenGeneration;
using GestorInventario.Interfaces.Application.Services.Authentication.TokenGeneration.Generators;
using GestorInventario.Shared.DTOS.Auth;

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
            var tareaTokenPrincipal = _tokenGenerator.GenerateTokenAsync(credencialesUsuario);
            var tareaTokenRefresco = _refreshTokenGenerator.GenerateTokenAsync(credencialesUsuario);

            await Task.WhenAll(tareaTokenPrincipal, tareaTokenRefresco);

            var tokenPrincipal = tareaTokenPrincipal.Result;
            var tokenRefresco = tareaTokenRefresco.Result;

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
