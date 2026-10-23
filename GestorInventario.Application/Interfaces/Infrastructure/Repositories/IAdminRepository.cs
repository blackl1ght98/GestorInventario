using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Infrastructure.Repositories
{
    public interface IAdminRepository
    {
        //Consulta
        IQueryable<Role> ObtenerRoles();
        string? ObtenerNombreRolPorId(int idRol);
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
