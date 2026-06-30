
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.DTOS.User;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Services.Authentication.Token_generation
{

    public class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        private readonly TokenStrategyResolver _resolver;
        private readonly IUserRepository _userRepository;
        public RefreshTokenGenerator(IUserRepository userRepository, TokenStrategyResolver resolver)
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
