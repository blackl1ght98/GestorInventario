using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Services.Authentication
{

    public class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        private readonly ITokenStrategyResolver _resolver;
        private readonly IAdminRepository _adminRepository;
        public RefreshTokenGenerator(IAdminRepository adminRepository, ITokenStrategyResolver resolver)
        {
            _resolver = resolver;
            _adminRepository=adminRepository;
        }

        public async Task<string> GenerateTokenAsync(Usuario credencialesUsuario)
        {
            var usuarioDB = await _adminRepository.ObtenerUsuarioPorId(credencialesUsuario.Id);
            return await _resolver.ResolveRefreshToken().GenerarTokenRefresco(usuarioDB);
        }
    }
}
