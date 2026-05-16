using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAdminRepository
    {
        //Consulta
        IQueryable<Role> ObtenerRoles();
        IQueryable<Usuario> ObtenerUsuarios();
        IQueryable<Usuario> ObtenerUsuariosPorRol(int rolId);
        Task<Usuario> ObtenerUsuarioConProveedoresYPedidosAsync(int id);
        Task<List<string>> ObtenerEmailsAdministradoresAsync(CancellationToken cancellationToken = default);
        //Operaciones
        Task<OperationResult<string>> EliminarUsuario(int id);  
        Task<OperationResult<string>> BajaUsuario(int id);
        Task<OperationResult<string>> AltaUsuario(int id);
        Task<OperationResult<Usuario>> ActualizarRolUsuario(int usuarioId, int rolId);   
        Task<OperationResult<string>> ReasignarProveedoresAsync(int usuarioOrigenId, int usuarioDestinoId);
  

    }
}
