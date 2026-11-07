using System.Security.Claims;

namespace GestorInventario.Application.Interfaces.Web
{
    public interface ICurrentUserAccessor
    {
        int GetCurrentUserId();
        string? GetCurrentUserEmail();
    
        ClaimsPrincipal? GetPrincipal();
        string GetClientIpAddress();
        string GetRequestMethod();
        bool IsAuthenticated();
        bool IsAdmin();
        bool IsAdminOrUser();
    }
}
