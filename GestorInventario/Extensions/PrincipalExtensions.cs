using System.Security.Claims;

using GestorInventario.Domain.enums.Usuario;


namespace GestorInventario.Extensions
{
    public static class PrincipalExtensions
    {
     
        private static string AdministradorRole =Rol.Administrador.ToString();

        public static bool IsAdministrador(this ClaimsPrincipal user)
            => user.IsInRole(AdministradorRole);
    }
}