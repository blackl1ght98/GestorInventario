using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Authentication
{
    public interface IRefreshTokenGenerator
    {
        Task<string> GenerateTokenAsync(Usuario credencialesUsuario);
    }
}
