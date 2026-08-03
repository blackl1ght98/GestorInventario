using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Auth;

namespace GestorInventario.Interfaces.Application.Services.Authentication.TokenGeneration.Generators
{
    public interface ITokenGenerator
    { 
        Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario);          
    }
}
