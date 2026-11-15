using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Services.Authentication
{
    public class LoginService : ILoginService
    {
        private readonly ILoginStrategyResolver _resolver;

        public LoginService(ILoginStrategyResolver resolver)
        {
            _resolver = resolver;
        }

        public async Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto credencialesUsuario)
        {
            return await _resolver.Resolve().AuthenticateAsync(credencialesUsuario);
        }
    }
}
