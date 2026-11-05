using GestorInventario.Application.DTOS.Auth;

namespace GestorInventario.Application.Interfaces.Authentication
{
    public interface IHashService
    {
        HashResult Hash(string password);
        HashResult Hash(string password, byte[] salt);
    }
}
