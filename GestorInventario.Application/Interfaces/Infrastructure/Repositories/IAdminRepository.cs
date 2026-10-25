using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Infrastructure.Repositories
{
    public interface IAdminRepository
    {
        //Consulta
 
      
        IQueryable<Usuario> ObtenerUsuarios();
        IQueryable<Usuario> ObtenerUsuariosPorRol(int rolId);
        Task<Usuario> ObtenerUsuarioConProveedoresYPedidosAsync(int id);
        Task<List<string>> ObtenerEmailsAdministradoresAsync(CancellationToken stoppingToken = default);

        Task<Usuario> ObtenerUsuarioPorId(int id);

        Task<Usuario> ObtenerEmail(string email);

        Task<bool> AnyAsync();

        Task<List<string>> ObtenerEmailsAdministradoresAsync();

        Task<bool> ExisteEmailAsync(string email);

        Task<OperationResult<Usuario>> EliminarUsuarioAsync(Usuario usuario);

     
        Task<OperationResult<Usuario>> AgregarUsuarioAsync(Usuario usuario);
        Task<List<Usuario>> ObtenerUsuariosAsync();
        
        //Operaciones

        
        Task<OperationResult<string>> ActualizarUsuarioAsync(Usuario usuario);
        Task<OperationResult<string>> ReasignarProveedoresAsync(int usuarioOrigenId, int usuarioDestinoId);
  

    }
}
