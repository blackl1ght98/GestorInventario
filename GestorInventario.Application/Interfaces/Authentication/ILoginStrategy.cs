using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Authentication
{
    public interface ILoginStrategy
    {
        Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto model);
    }
}
