using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Services.Authentication
{
    public class TokenGenerator : ITokenGenerator
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenStrategyResolver _resolver;

        public TokenGenerator(IUserRepository userRepository, ITokenStrategyResolver resolver)
        {
          _userRepository = userRepository;
            _resolver = resolver;
        }

        public async Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            var usuarioDB = await _userRepository.ObtenerUsuarioPorId(credencialesUsuario.Id);

            if (usuarioDB is null)
                throw new ArgumentException("El usuario no existe en la base de datos.");

            return await _resolver.ResolveToken().GenerateTokenAsync(usuarioDB);
        }
    }
}