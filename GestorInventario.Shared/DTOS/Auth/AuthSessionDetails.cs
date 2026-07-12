using GestorInventario.Domain.Models;

namespace GestorInventario.Shared.DTOS.Auth
{
    public record AuthSessionDetails(
     Usuario User,
     bool RequiresMfa = false,
     string? MfaCode = null
 );
}
