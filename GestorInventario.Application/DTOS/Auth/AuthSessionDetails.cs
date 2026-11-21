using GestorInventario.Domain.Models;

namespace GestorInventario.Application.DTOS.Auth
{
    public record AuthSessionDetails(
     Usuarios User,
     bool RequiresMfa = false
 );
}
