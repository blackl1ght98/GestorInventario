using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        //Consulta
 
      
        IQueryable<Usuarios> ObtenerUsuarios();
        IQueryable<Usuarios> ObtenerUsuariosPorRol(int rolId);
        Task<Usuarios> ObtenerUsuarioConProveedoresYPedidosAsync(int id);
        Task<List<string>> ObtenerEmailsAdministradoresAsync(CancellationToken stoppingToken = default);

        Task<Usuarios> ObtenerUsuarioPorId(int id);

        Task<Usuarios> ObtenerEmail(string email);

        Task<bool> AnyAsync();

        Task<List<string>> ObtenerEmailsAdministradoresAsync();

        Task<bool> ExisteEmailAsync(string email);

        Task<OperationResult<Usuarios>> EliminarUsuarioAsync(Usuarios usuario);

     
        Task<OperationResult<Usuarios>> AgregarUsuarioAsync(Usuarios usuario);
        Task<List<Usuarios>> ObtenerUsuariosAsync();

        Task<Usuarios> ExisteUsuarioConEmailAsync(string email);
        //Operaciones

        
        Task<OperationResult<string>> ActualizarUsuarioAsync(Usuarios usuario);
        Task<OperationResult<string>> ReasignarProveedoresAsync(int usuarioOrigenId, int usuarioDestinoId);
  

    }
}
