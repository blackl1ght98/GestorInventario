using GestorInventario.Interfaces.Application;
using System.Net.Sockets;
using System.Security.Claims;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class CurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public CurrentUserAccessor(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public int GetCurrentUserId()
        {
            var value = _contextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(value) || !int.TryParse(value, out var id))
            {
                throw new InvalidOperationException("No se pudo obtener el ID del usuario autenticado.");
            }
            return id;
        }

        public string? GetCurrentUserEmail()
        {
            return _contextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
        }

        public bool IsInRole(string role)
        {
            return _contextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
        }

        public ClaimsPrincipal? GetPrincipal() => _contextAccessor.HttpContext?.User;
        public string GetClientIpAddress()
        {
            var httpContext = _contextAccessor.HttpContext;
            if (httpContext == null)
            {
                return string.Empty;
            }

            var ip = httpContext.Connection.RemoteIpAddress;
            if (ip == null)
            {
                return string.Empty;
            }

            // Convierte IPv6 a IPv4 si es mapeado (ej: "::ffff:192.168.1.1")
            return ip.AddressFamily == AddressFamily.InterNetworkV6
                ? ip.MapToIPv4().ToString()
                : ip.ToString();
        }
        public string GetRequestMethod()
        {
            return _contextAccessor.HttpContext?.Request?.Method ?? "Unknown";
        }
        public bool IsAuthenticated()
        {
            return _contextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
        }
    }
}
