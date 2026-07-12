using System.Security.Claims;


namespace GestorInventario.Extensions
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
