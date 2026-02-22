using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAuthRepository
    {
        Task<OperationResult<Usuario>> Login(string email);
        Task<OperationResult<string>> SetNewPasswordAsync(RestoresPasswordDto cambio);
        Task<OperationResult<string>> ChangePassword(string passwordAnterior, string passwordActual);  
        Task<OperationResult<RestoresPasswordDto>> PrepareRestorePassModel(int userId, string token);
    }
}
