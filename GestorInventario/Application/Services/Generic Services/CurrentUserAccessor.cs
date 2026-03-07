using GestorInventario.Interfaces.Application;
using Microsoft.AspNetCore.Authentication.Cookies;
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
            var principal = _contextAccessor.HttpContext?.User;

            if (principal == null || !principal.Identity.IsAuthenticated)
            {
                LimpiarCookiesCompletamente();
                return 0;
            }

            var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(value) || !int.TryParse(value, out var id))
            {
                LimpiarCookiesCompletamente();
                return 0;
            }

            return id;
        }
        private void LimpiarCookiesCompletamente()
        {
            var httpContext = _contextAccessor.HttpContext;
            if (httpContext == null) return;

            // Borrar todas las cookies
            foreach (var cookie in httpContext.Request.Cookies)
            {
                httpContext.Response.Cookies.Delete(cookie.Key);
            }

            // Borrar específicamente la cookie de autenticación
            httpContext.Response.Cookies.Delete(".AspNetCore.Antiforgery.VVHw_PaTSjY");          
            httpContext.Response.Cookies.Delete("auth");         // si usas nombre custom
            httpContext.Response.Cookies.Delete("refreshToken");
            httpContext.Response.Cookies.Delete(CookieAuthenticationDefaults.CookiePrefix + "Application");
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
