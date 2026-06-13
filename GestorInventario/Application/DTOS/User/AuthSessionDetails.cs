using GestorInventario.Domain.Models;

namespace GestorInventario.Application.DTOS.User
{
    public record AuthSessionDetails(
     Usuario User,
     bool RequiresMfa = false,
     string? MfaCode = null
 );
}
