using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Authentication
{
    public interface IRefreshTokenService
    {
        Task<string> GenerateTokenAsync(Usuario credencialesUsuario);
    }
}
