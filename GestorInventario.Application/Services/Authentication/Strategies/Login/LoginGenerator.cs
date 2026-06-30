
using GestorInventario.Domain.Models;

using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Shared.DTOS.User;
using GestorInventario.Shared.Utilities;
using GestorInventario.Utilities;


namespace GestorInventario.Application.Services.Authentication.Strategies.Login
{
    public class LoginGenerator : ILoginGenerator
    {
        private readonly LoginStrategyResolver _resolver;

        public LoginGenerator(LoginStrategyResolver resolver)
        {
            _resolver = resolver;
        }

        public async Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto credencialesUsuario)
        {
            return await _resolver.Resolve().AuthenticateAsync(credencialesUsuario);
        }
    }
}
