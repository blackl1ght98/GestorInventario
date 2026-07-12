using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Auth;

namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface ITokenGenerator
    { 
        Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario);          
    }
}
