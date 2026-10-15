using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Services.Authentication.TokenGeneration
{
    public interface ITokenService
    {
        Task<LoginResponseDto> GenerarToken(Usuario credencialesUsuario);
    }
}
