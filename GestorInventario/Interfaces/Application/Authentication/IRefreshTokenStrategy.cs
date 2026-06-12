using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application
{
    public interface IRefreshTokenStrategy
    {
        Task<string> GenerarTokenRefresco(Usuario credencialesUsuario);
    }
}
