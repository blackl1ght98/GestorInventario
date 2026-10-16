using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Application.Services.Authentication.TokenGeneration.Generators
{
    public interface IRefreshTokenGenerator
    {
        Task<string> GenerateTokenAsync(Usuario credencialesUsuario);
    }
}
