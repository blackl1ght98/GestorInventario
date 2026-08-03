using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application.Services.Authentication.Strategies.RefreshToken
{
    public interface IRefreshTokenStrategy
    {
        Task<string> GenerarTokenRefresco(Usuario credencialesUsuario);
    }
}
