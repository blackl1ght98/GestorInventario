using GestorInventario.Domain.enums.Usuario;

namespace GestorInventario.Application.Constants;

public class RolesConstants
{
    public const string Administrador = nameof(Rol.Administrador);
    public const string Usuario = nameof(Rol.Usuario);
    public const string DefaultRegistro = Usuario; 
}