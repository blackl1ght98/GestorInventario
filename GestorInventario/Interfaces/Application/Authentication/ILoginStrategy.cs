using GestorInventario.Application.DTOS.User;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.Usuarios;

namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface ILoginStrategy
    {
        Task<OperationResult<AuthSessionDetails>> AuthenticateAsync(LoginViewModel model);
    }
}
