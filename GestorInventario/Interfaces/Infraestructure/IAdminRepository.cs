using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.user;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAdminRepository
    {
        OperationResult<IQueryable<Role>> ObtenerRoles();
        IQueryable<Usuario> ObtenerUsuarios();
        Task<OperationResult<string>> EliminarUsuario(int id);
    
        Task<OperationResult<string>> BajaUsuario(int id);
        Task<OperationResult<string>> AltaUsuario(int id);
        Task<OperationResult<Usuario>> ActualizarRolUsuario(int usuarioId, int rolId);
        IQueryable<Usuario> ObtenerUsuariosPorRol(int rolId);
        Task<OperationResult<string>> ReasignarProveedoresAsync(int usuarioOrigenId, int usuarioDestinoId);
        Task<OperationResult<Usuario>> ObtenerUsuarioConProveedoresYPedidosAsync(int id);

    }
}
