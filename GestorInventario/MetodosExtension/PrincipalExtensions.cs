using System.Security.Claims;
using System.Security.Principal;

namespace GestorInventario.MetodosExtension
{
    public static class PrincipalExtensions
    {
        private const string AdministradorRole= "Administrador";
        public static bool IsAdministrador(this ClaimsPrincipal user)
        {
            return user.IsInRole(AdministradorRole);
        }
    }
}
