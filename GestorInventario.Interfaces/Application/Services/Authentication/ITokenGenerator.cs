using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Auth;

namespace GestorInventario.Interfaces.Application.Services.Authentication
{
    public interface ITokenGenerator
    { 
        Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario);          
    }
}
