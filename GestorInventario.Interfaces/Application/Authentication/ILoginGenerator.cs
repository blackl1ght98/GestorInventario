using GestorInventario.Shared.DTOS.User;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface ILoginGenerator
    {
        Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto credencialesUsuario);
    }
}
