using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.user;

namespace GestorInventario.Interfaces.Application
{
    public interface IUserManagementService
    {
        Task<OperationResult<string>> CrearUsuarioAsync(UserViewModel model);
        Task<OperationResult<string>> EditarUsuarioAsync(UsuarioEditViewModel userVM);
        Task<OperationResult<string>> EliminarUsuarioAsync(int id);
    }
}
