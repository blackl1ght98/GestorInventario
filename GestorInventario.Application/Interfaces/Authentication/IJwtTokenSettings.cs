using System.Security.Claims;
using System.Security.Cryptography;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Authentication
{
    public interface IJwtTokenSettings
    {
        IReadOnlyList<Claim> CrearClaims(Usuario usuario);
        string ObtenerIssuer();
        string ObtenerAudience();
        int ObtenerDuracionAccessTokenMinutos();
        int ObtenerDuracionRefreshTokenHoras();
        RSA ObtenerPublicKeyFixed();
        string ObtenerClaveJWT();
        string ObtenerClavePrivada();

    }
}
