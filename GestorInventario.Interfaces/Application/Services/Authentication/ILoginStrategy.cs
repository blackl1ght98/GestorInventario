using GestorInventario.Shared.DTOS.Auth;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Interfaces.Application.Services.Authentication
{
    public interface ILoginStrategy
    {
        Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto model);
    }
}
