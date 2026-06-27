using GestorInventario.Application.DTOs.User;

using GestorInventario.Utilities;
using GestorInventario.ViewModels.Usuarios;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IUserManagementService
    {
        Task<OperationResult<string>> CrearUsuarioAsync(UserViewModel model);
        Task<OperationResult<string>> EditarUsuarioAsync(UsuarioEditViewModel userVM);
        Task<OperationResult<string>> EliminarUsuarioAsync(int id);
        Task<OperationResult<string>> ValidarRegistro(ConfirmRegistrationDto confirmar);
    }
}
