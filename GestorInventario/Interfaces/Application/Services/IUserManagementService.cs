using GestorInventario.Application.DTOs.User;
using GestorInventario.Application.DTOS.User;
using GestorInventario.Utilities;
using GestorInventario.ViewModels.Usuarios;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IUserManagementService
    {
        Task<OperationResult<string>> CrearUsuarioAsync(RegisterUserDto model);
        Task<OperationResult<string>> EditarUsuarioAsync(EditUserDto userVM);
        Task<OperationResult<string>> EliminarUsuarioAsync(int id);
        Task<OperationResult<string>> ValidarRegistro(ConfirmRegistrationDto confirmar);
    }
}
