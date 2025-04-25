using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application
{
    public interface IRefreshTokenMethod
    {
        Task<string> GenerarTokenRefresco(Usuario credencialesUsuario);


    }
}
