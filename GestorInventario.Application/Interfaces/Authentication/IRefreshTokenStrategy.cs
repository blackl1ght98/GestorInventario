using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Authentication
{
    public interface IRefreshTokenStrategy
    {
        Task<string> GenerarTokenRefresco(Usuario credencialesUsuario);
    }
}
