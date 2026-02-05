using System.Security.Claims;

namespace GestorInventario.Interfaces.Application
{
    public interface ICurrentUserAccessor
    {
        int GetCurrentUserId();
        string? GetCurrentUserEmail();
        bool IsInRole(string role);
        ClaimsPrincipal? GetPrincipal();
        string GetClientIpAddress();
        string GetRequestMethod();
    }
}
