using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAuthRepository
    {
        Task<Usuario> Login(string email);      
        Task<Usuario> ObtenerPorId(int id);
        Task<(bool, string)> ValidateResetTokenAsync(RestoresPasswordDto cambio);
        Task<(bool, string)> SetNewPasswordAsync(RestoresPasswordDto cambio);
        Task<(bool, string)> ChangePassword(string passwordAnterior, string passwordActual);
        Task EliminarCarritoActivo();
    }
}
