using GestorInventario.Application.DTOS.User;

using GestorInventario.Utilities;
using GestorInventario.ViewModels.Usuarios;

namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface ILoginGenerator
    {
        Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginViewModel credencialesUsuario);
    }
}
