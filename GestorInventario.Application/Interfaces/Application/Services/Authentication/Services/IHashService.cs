using GestorInventario.Application.DTOS.Auth;

namespace GestorInventario.Application.Interfaces.Application.Services.Authentication.Services
{
    public interface IHashService
    {
        HashResult Hash(string password);
        HashResult Hash(string password, byte[] salt);
    }
}
