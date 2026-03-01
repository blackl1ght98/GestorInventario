using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application
{
    public interface ITokenGenerator
    { 
        Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario);          
    }
}
