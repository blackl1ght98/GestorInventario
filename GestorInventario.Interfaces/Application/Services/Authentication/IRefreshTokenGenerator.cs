using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application.Services.Authentication
{
    public interface IRefreshTokenGenerator
    {
        Task<string> GenerateTokenAsync(Usuario credencialesUsuario);
    }
}
