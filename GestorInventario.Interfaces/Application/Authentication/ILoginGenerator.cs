using GestorInventario.Shared.DTOS.Auth;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface ILoginGenerator
    {
        Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto credencialesUsuario);
    }
}
