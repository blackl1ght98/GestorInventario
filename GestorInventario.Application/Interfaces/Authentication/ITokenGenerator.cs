using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Authentication
{
    public interface ITokenGenerator
    { 
        Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario);          
    }
}
