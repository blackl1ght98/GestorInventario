using GestorInventario.Domain.Models;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GestorInventario.Interfaces.Application.Authentication
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
