using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.user;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAuthRepository
    {
        Task<OperationResult<Usuario>> Login(string email, LoginViewModel model);
        Task<OperationResult<string>> SetNewPasswordAsync(RestoresPasswordDto cambio);
        Task<OperationResult<string>> ChangePassword(string passwordAnterior, string passwordActual);  
        Task<OperationResult<RestoresPasswordDto>> PrepareRestorePassModel(int userId, string token);
        Task<OperationResult<string>> EnviarCorreoResetAsync(string email);
    }
}
