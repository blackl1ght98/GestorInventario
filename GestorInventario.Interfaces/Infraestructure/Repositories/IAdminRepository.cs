using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Infraestructure.Repositories
{
    public interface IAdminRepository
    {
        //Consulta
        IQueryable<Role> ObtenerRoles();
        IQueryable<Usuario> ObtenerUsuarios();
        IQueryable<Usuario> ObtenerUsuariosPorRol(int rolId);
        Task<Usuario> ObtenerUsuarioConProveedoresYPedidosAsync(int id);
        Task<List<string>> ObtenerEmailsAdministradoresAsync(CancellationToken stoppingToken = default);
        //Operaciones
        Task<OperationResult<string>> EliminarUsuario(int id);  
        Task<OperationResult<string>> BajaUsuario(int id);
        Task<OperationResult<string>> AltaUsuario(int id);
        Task<OperationResult<Usuario>> ActualizarRolUsuario(int usuarioId, int rolId);   
        Task<OperationResult<string>> ReasignarProveedoresAsync(int usuarioOrigenId, int usuarioDestinoId);
  

    }
}
