using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Services.Authentication
{
    public class LoginService : ILoginService
    {
        private readonly ILoginStrategyResolver _resolver;
        private readonly IUserRepository _userRepository;
        public LoginService(ILoginStrategyResolver resolver,IUserRepository  userRepository)
        {
            _resolver = resolver;
            _userRepository = userRepository;
        }

        public async Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto credencialesUsuario)
        {
            var user = await _userRepository.ExisteUsuarioConEmailAsync(credencialesUsuario.Email);
            return await _resolver.Resolve(user.IdRolNavigation.Nombre).AuthenticateAsync(credencialesUsuario);
        }
    }
}
