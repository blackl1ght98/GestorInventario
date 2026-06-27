using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Application.Services.Authentication.Token_generation
{
    public class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenStrategy _refreshStrategy;

        public RefreshTokenGenerator(IUserRepository userRepository, IRefreshTokenStrategy refreshStrategy)
        {
            _userRepository = userRepository;
            _refreshStrategy = refreshStrategy;
        }

        public async Task<string> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            var usuarioDB = await _userRepository.ObtenerUsuarioPorId(credencialesUsuario.Id);

            if (usuarioDB is null)
                throw new ArgumentException("El usuario no existe en la base de datos.");

            return await _refreshStrategy.GenerarTokenRefresco(usuarioDB);
        }
    }
}
