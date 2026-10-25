using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.Resolvers;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.TokenGeneration.Generators;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;

using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Services.Authentication.TokenGeneration.Generators
{
    public class TokenGenerator : ITokenGenerator
    {
        private readonly IAdminRepository _adminRepository;
        private readonly ITokenStrategyResolver _resolver;

        public TokenGenerator(IAdminRepository adminRepository, ITokenStrategyResolver resolver)
        {
          _adminRepository = adminRepository;
            _resolver = resolver;
        }

        public async Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            var usuarioDB = await _adminRepository.ObtenerUsuarioPorId(credencialesUsuario.Id);

            if (usuarioDB is null)
                throw new ArgumentException("El usuario no existe en la base de datos.");

            return await _resolver.ResolveToken().GenerateTokenAsync(usuarioDB);
        }
    }
}