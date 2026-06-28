using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface IRefreshTokenStrategy
    {
        Task<string> GenerarTokenRefresco(Usuario credencialesUsuario);
    }
}
