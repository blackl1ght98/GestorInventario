using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.User;

namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface ITokenService
    {
        Task<LoginResponseDto> GenerarToken(Usuario credencialesUsuario);
    }
}
