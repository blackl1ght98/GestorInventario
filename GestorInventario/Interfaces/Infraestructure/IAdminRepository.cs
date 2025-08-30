using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.ViewModels.user;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAdminRepository
    {

        IQueryable<Usuario> ObtenerUsuarios();
        Task<(Usuario?, string)> ObtenerPorId(int id);
        Task<List<Role>> ObtenerRoles();      
        IQueryable<Role> ObtenerRolesConUsuarios();
        Task<(Usuario?, string)> ObtenerUsuarioConPedido(int id);
        IQueryable<Usuario> ObtenerUsuariosPorRol(int rolId);
        Task<List<Permiso>> ObtenerPermisos();
        Task<(bool, string?)> EditarUsuario(UsuarioEditViewModel userVM);   
        Task<(bool, string)> CrearUsuario(UserViewModel model);
        Task<(bool, string)> EliminarUsuario(int id);        
        Task<(bool, string)> BajaUsuario(int id);
        Task<(bool, string)> AltaUsuario(int id);

       
        Task ActualizarRolUsuario(int usuarioId, int rolId);
        Task<(bool, string)> CrearRol(string nombreRol, List<int> permisoIds);
  
        Task<(bool, List<int>, string)> CrearPermisos(List<NewPermisoDTO> nuevosPermisos);
    }
}
