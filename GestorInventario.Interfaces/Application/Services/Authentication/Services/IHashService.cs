using GestorInventario.Shared.DTOS.Auth;

namespace GestorInventario.Interfaces.Application.Services.Authentication.Services
{
    public interface IHashService
    {
        HashResult Hash(string password);
        HashResult Hash(string password, byte[] salt);
    }
}
