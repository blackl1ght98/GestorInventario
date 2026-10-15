using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Services.Authentication.Strategies.RefreshToken
{
    public interface IRefreshTokenStrategy
    {
        Task<string> GenerarTokenRefresco(Usuario credencialesUsuario);
    }
}
