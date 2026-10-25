using GestorInventario.Application.DTOS.User;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.User
{
    public interface IUserService
    {
        Task<OperationResult<string>> CrearUsuarioAsync(RegisterUserDto model, string? rolSolicitado = null);
        Task<OperationResult<string>> EditarUsuarioAsync(EditUserDto userVm);
        Task<OperationResult<Usuario>> EliminarUsuarioAsync(int id);
        Task<OperationResult<string>> ValidarRegistro(ConfirmRegistrationDto confirmar);
        Task<OperationResult<string>> CambiarEstadoUsuarioAsync(int id, bool activar);

    }
}
