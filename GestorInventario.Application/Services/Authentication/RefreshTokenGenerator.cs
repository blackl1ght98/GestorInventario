using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Services.Authentication
{

    public class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        private readonly ITokenStrategyResolver _resolver;
        private readonly IUserRepository _userRepository;
        public RefreshTokenGenerator(IUserRepository userRepository, ITokenStrategyResolver resolver)
        {
            _resolver = resolver;
            _userRepository=userRepository;
        }

        public async Task<string> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            var usuarioDB = await _userRepository.ObtenerUsuarioPorId(credencialesUsuario.Id);
            return await _resolver.ResolveRefreshToken().GenerarTokenRefresco(usuarioDB);
        }
    }
}
