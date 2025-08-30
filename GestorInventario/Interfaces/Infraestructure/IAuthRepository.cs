using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAuthRepository
    {
        Task<(Usuario?, string)> Login(string email);
      
        Task<(bool, string)> ValidateResetTokenAsync(RestoresPasswordDto cambio);
        Task<(bool, string)> SetNewPasswordAsync(RestoresPasswordDto cambio);
        Task<(bool, string)> ChangePassword(string passwordAnterior, string passwordActual);
        Task EliminarCarritoActivo();
        Task<(bool, string, RestoresPasswordDto?)> PrepareRestorePassModel(int userId, string token);
    }
}
