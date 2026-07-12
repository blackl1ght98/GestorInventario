using GestorInventario.Shared.DTOS.Auth;

namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface IHashService
    {
        ResultadoHash Hash(string password);
        ResultadoHash Hash(string password, byte[] salt);
    }
}
