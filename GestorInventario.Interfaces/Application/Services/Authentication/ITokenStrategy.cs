using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Auth;

namespace GestorInventario.Interfaces.Application.Services.Authentication
{
    public interface ITokenStrategy
    {
        Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario);
    }
}
