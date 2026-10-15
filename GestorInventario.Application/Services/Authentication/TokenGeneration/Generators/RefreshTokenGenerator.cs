using GestorInventario.Application.Interfaces.Services.Authentication.Resolvers;
using GestorInventario.Application.Interfaces.Services.Authentication.TokenGeneration.Generators;
using GestorInventario.Application.Services.Authentication.Resolvers;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure.Repositories;


namespace GestorInventario.Application.Services.Authentication.TokenGeneration.Generators
{

    public class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        private readonly ITokenStrategyResolver _resolver;
        private readonly IUserRepository _userRepository;
        public RefreshTokenGenerator(IUserRepository userRepository, ITokenStrategyResolver resolver)
        {
            _resolver = resolver;
            _userRepository = userRepository;
        }

        public async Task<string> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            var usuarioDB = await _userRepository.ObtenerUsuarioPorId(credencialesUsuario.Id);
            return await _resolver.ResolveRefreshToken().GenerarTokenRefresco(usuarioDB);
        }
    }
}
