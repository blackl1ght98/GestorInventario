using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Infraestructure.Utils;
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

        Task<OperationResult<string>> EditarUsuario(UsuarioEditViewModel userVM);
        Task<OperationResult<string>> CrearUsuario(UserViewModel model);
        Task<OperationResult<string>> EliminarUsuario(int id);
        Task<OperationResult<string>> BajaUsuario(int id);
        Task<OperationResult<string>> AltaUsuario(int id);

       
        Task ActualizarRolUsuario(int usuarioId, int rolId);
     
  
     
    }
}
