

using GestorInventario.Domain.Models;

using GestorInventario.Infraestructure.Utils;

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
