using GestorInventario.Shared.DTOS.User;
using GestorInventario.Shared.Utilities;

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
