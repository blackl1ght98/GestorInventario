using GestorInventario.Application.DTOs.User;

namespace GestorInventario.Interfaces.Application
{
    public interface IHashService
    {
        ResultadoHash Hash(string password);
        ResultadoHash Hash(string password, byte[] salt);
    }
}
