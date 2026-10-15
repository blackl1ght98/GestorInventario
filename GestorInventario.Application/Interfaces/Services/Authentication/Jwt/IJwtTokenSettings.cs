using System.Security.Claims;
using System.Security.Cryptography;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Interfaces.Services.Authentication.Jwt
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

    }
}
