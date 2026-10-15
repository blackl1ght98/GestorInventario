using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Services.Authentication.Strategies.Login
{
    public interface ILoginStrategy
    {
        Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto model);
    }
}
