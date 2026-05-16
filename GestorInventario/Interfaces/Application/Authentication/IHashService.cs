using GestorInventario.Application.DTOs.User;

namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface IHashService
    {
        ResultadoHash Hash(string password);
        ResultadoHash Hash(string password, byte[] salt);
    }
}
