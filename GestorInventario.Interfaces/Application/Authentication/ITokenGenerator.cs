using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.User;

namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface ITokenGenerator
    { 
        Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario);          
    }
}
