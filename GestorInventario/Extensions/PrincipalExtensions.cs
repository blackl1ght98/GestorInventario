using System.Security.Claims;
using GestorInventario.Application.Services.User;

namespace GestorInventario.Extensions
{
    public static class PrincipalExtensions
    {
     
        private const string AdministradorRole = Roles.Administrador;

        public static bool IsAdministrador(this ClaimsPrincipal user)
            => user.IsInRole(AdministradorRole);
    }
}