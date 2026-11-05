using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Authentication
{
    public interface ITokenService
    {
        Task<LoginResponseDto> GenerarToken(Usuario credencialesUsuario);
    }
}
