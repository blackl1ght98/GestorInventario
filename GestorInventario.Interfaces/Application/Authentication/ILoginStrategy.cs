using GestorInventario.Shared.DTOS.User;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface ILoginStrategy
    {
        Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto model);
    }
}
