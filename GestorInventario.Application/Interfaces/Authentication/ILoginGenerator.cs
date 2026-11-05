using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Authentication
{
    public interface ILoginGenerator
    {
        Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto credencialesUsuario);
    }
}
