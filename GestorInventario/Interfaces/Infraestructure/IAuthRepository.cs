using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAuthRepository
    {
        Task<(Usuario?, string)> Login(string email);

        Task<OperationResult<string>> ValidateResetTokenAsync(RestoresPasswordDto cambio);
        Task<OperationResult<string>> SetNewPasswordAsync(RestoresPasswordDto cambio);
        Task<OperationResult<string>> ChangePassword(string passwordAnterior, string passwordActual);
        Task EliminarCarritoActivo();
        Task<OperationResult<RestoresPasswordDto>> PrepareRestorePassModel(int userId, string token);
    }
}
