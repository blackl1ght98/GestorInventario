using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Services.Authentication.TokenGeneration.Generators
{
    public interface IRefreshTokenGenerator
    {
        Task<string> GenerateTokenAsync(Usuario credencialesUsuario);
    }
}
