using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface IRefreshTokenGenerator
    {
        Task<string> GenerateTokenAsync(Usuario credencialesUsuario);
    }
}
