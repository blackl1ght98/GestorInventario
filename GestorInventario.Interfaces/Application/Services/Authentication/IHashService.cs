using GestorInventario.Shared.DTOS.Auth;

namespace GestorInventario.Interfaces.Application.Services.Authentication
{
    public interface IHashService
    {
        ResultadoHash Hash(string password);
        ResultadoHash Hash(string password, byte[] salt);
    }
}
