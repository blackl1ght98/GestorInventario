using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application
{
    public interface ITokenStrategy
    {
        Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario);
    }
}
