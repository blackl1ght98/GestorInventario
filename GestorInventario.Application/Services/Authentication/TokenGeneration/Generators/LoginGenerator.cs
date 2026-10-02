using GestorInventario.Application.Services.Authentication.Resolvers;
using GestorInventario.Interfaces.Application.Services.Authentication.Resolvers;
using GestorInventario.Interfaces.Application.Services.Authentication.TokenGeneration.Generators;
using GestorInventario.Shared.DTOS.Auth;
using GestorInventario.Shared.Utilities;



namespace GestorInventario.Application.Services.Authentication.TokenGeneration.Generators
{
    public class LoginGenerator : ILoginGenerator
    {
        private readonly ILoginStrategyResolver _resolver;

        public LoginGenerator(ILoginStrategyResolver resolver)
        {
            _resolver = resolver;
        }

        public async Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto credencialesUsuario)
        {
            return await _resolver.Resolve().AuthenticateAsync(credencialesUsuario);
        }
    }
}
