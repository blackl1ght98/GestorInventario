using GestorInventario.Application.DTOS.User;

using GestorInventario.Utilities;
using GestorInventario.ViewModels.Usuarios;

namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface ILoginStrategy
    {
        Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginDto model);
    }
}
