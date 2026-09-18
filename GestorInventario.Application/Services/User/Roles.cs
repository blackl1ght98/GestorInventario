using GestorInventario.Domain.enums.Usuario;


namespace GestorInventario.Application.Services.User
{
    public static class Roles
    {
        public const string Administrador = nameof(Rol.Administrador);
        public const string Usuario = nameof(Rol.Usuario);
        public const string DefaultRegistro = Usuario; 
    }
}
