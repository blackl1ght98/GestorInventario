using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Services.Authentication
{
    public class TokenService : ITokenService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenStrategyResolver _tokenStrategyResolver;
        private readonly IRefreshTokenService _refreshTokenService;

        public TokenService(
            IUserRepository userRepository,
            ITokenStrategyResolver tokenStrategyResolver,
            IRefreshTokenService refreshTokenService)
        {
            _userRepository = userRepository;
            _tokenStrategyResolver = tokenStrategyResolver;
            _refreshTokenService = refreshTokenService;
        }

        public async Task<LoginResponseDto> GenerarToken(Usuarios credencialesUsuario)
        {
            // 1. Obtener el usuario fresco de la base de datos
            var usuarioDB = await _userRepository.ObtenerUsuarioPorId(credencialesUsuario.Id);
            if (usuarioDB is null)
                throw new ArgumentException("El usuario no existe en la base de datos.");

            // 2. Generar el Access Token usando la estrategia correspondiente
            var tokenPrincipal = await _tokenStrategyResolver.ResolveToken().GenerateTokenAsync(usuarioDB);

            // 3. Generar el Refresh Token
            var tokenRefresco = await _refreshTokenService.GenerateTokenAsync(usuarioDB);

            // 4. Combinar todo en la respuesta final
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
