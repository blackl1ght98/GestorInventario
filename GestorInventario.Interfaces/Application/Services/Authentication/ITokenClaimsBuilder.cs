using GestorInventario.Domain.Models;
using System.Security.Claims;
using System.Security.Cryptography;


namespace GestorInventario.Interfaces.Application.Services.Authentication
{
    public interface ITokenClaimsBuilder
    {
        IReadOnlyList<Claim> CrearClaims(Usuario usuario);
        string ObtenerIssuer();
        string ObtenerAudience();
        int ObtenerDuracionAccessTokenMinutos();
        int ObtenerDuracionRefreshTokenHoras();
        RSA ObtenerPublicKeyFixed();
        string ObtenerClaveJWT();

    }
}
