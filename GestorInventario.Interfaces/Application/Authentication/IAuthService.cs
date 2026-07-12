using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Auth;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface IAuthService
    {
        Task<OperationResult<Usuario>> Login(string email, LoginDto model);
        Task<OperationResult<string>> SetNewPasswordAsync(RestoresPasswordDto cambio);
        Task<OperationResult<string>> ChangePassword(string passwordAnterior, string passwordActual);
        Task<OperationResult<RestoresPasswordDto>> PrepareRestorePassModel(int userId, string token);
        Task<OperationResult<string>> EnviarCorreoResetAsync(string email);
    }
}
