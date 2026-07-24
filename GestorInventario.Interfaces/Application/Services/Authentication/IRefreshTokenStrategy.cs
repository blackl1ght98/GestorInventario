using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application.Services.Authentication
{
    public interface IRefreshTokenStrategy
    {
        Task<string> GenerarTokenRefresco(Usuario credencialesUsuario);
    }
}
