using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Services.Authentication.TokenGeneration.Generators
{
    public interface ITokenGenerator
    { 
        Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario);          
    }
}
