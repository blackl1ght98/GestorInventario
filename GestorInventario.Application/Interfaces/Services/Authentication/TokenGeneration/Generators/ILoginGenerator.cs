using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Services.Authentication.TokenGeneration.Generators
{
    public interface ILoginGenerator
    {
        Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto credencialesUsuario);
    }
}
