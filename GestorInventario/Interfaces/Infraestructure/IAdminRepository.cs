using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Models.ViewModels;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAdminRepository
    {
        //IQueryable<Usuario> ObtenerUsuarios();
        Task<IEnumerable<Usuario>> ObtenerUsuarios();
        Task<Usuario> ObtenerPorId(int id);
        Task<IEnumerable<Role>> ObtenerRoles();
        Task<Usuario> ObtenerUsuarioId(int id);
        Task<Usuario> UsuarioConPedido(int id);

        Task<(bool, string?)> EditarUsuario(UsuarioEditViewModel userVM);
        Task<(bool, string)> EditarRol(int id, int newRole);
        Task<(bool, string)> CrearUsuario(UserViewModel model);
        Task<(bool, string)> EliminarUsuario(int id);
        Task<(bool, string)> EditarUsuarioActual(EditarUsuarioActual userVM);
        Task<(bool, string)> BajaUsuario(int id);
        Task<(bool, string)> AltaUsuario(int id);
        Task<List<Usuario>> ObtenerTodosUsuarios();

    }
}
