using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Models.ViewModels;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAdminRepository
    {
        
        Task<IEnumerable<Usuario>> ObtenerUsuarios();
        Task<Usuario> ObtenerPorId(int id);
        Task<IEnumerable<Role>> ObtenerRoles();
        Task<Usuario> ObtenerUsuarioId(int id);
        Task<Usuario> UsuarioConPedido(int id);
        Task<(bool, string?)> EditarUsuario(UsuarioEditViewModel userVM);   
        Task<(bool, string)> CrearUsuario(UserViewModel model);
        Task<(bool, string)> EliminarUsuario(int id);
        Task<(bool, string)> EditarUsuarioActual(EditarUsuarioActual userVM);
        Task<(bool, string)> BajaUsuario(int id);
        Task<(bool, string)> AltaUsuario(int id);
        Task<List<Role>> ObtenerRolesConUsuarios();
        Task ActualizarRolUsuario(int usuarioId, int rolId);
        Task<(bool, string)> CrearRol(string nombreRol, List<int> permisoIds);
        Task<List<Permiso>> ObtenerPermisos() ;
        Task<(bool, List<int>, string)> CrearPermisos(List<NewPermisoDTO> nuevosPermisos);
    }
}
