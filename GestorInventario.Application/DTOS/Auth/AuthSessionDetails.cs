using GestorInventario.Domain.Models;

namespace GestorInventario.Application.DTOS.Auth
{
    public record AuthSessionDetails(
     Usuario User,
     bool RequiresMfa = false
 );
}
