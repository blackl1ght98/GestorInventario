using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Services.Authentication.Services
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
