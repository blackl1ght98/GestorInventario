using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application
{
    public interface ITokenService
    {
        Task<LoginResponseDto> GenerarToken(Usuario credencialesUsuario);
    }
}
