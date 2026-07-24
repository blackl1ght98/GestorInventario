using GestorInventario.Shared.DTOS.Auth;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Interfaces.Application.Services.Authentication
{
    public interface ILoginGenerator
    {
        Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto credencialesUsuario);
    }
}
