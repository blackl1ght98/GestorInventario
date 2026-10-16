using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Infrastructure.Repositories
{
    public interface IUserRepository
    {
        //Consultas
        Task<Usuario> ObtenerUsuarioPorId(int id);
        Task<Usuario> ObtenerEmail(string email);
        Task<List<Usuario>> ObtenerUsuariosAsync();
        Task<Usuario> ObtenerUsuarioConProveedoresYPedidosAsync(int id);
        Task<bool> ExisteEmailAsync(string email);
        Task<List<string>> ObtenerEmailsAdministradoresAsync();
        Task<List<Role>> GetAllAsync();
        Task<bool> AnyAsync();
        Task<Role?> GetRolByNameAsync(string nombre);
        Task<Role?> GetByIdRolAsync(int idRol);
        //Operaciones
        Task ConfirmEmail(int userId);
        Task<OperationResult<string>> ActualizarEmailVerificationTokenAsync(int userId, string token); 
        Task<OperationResult<Usuario>> AgregarUsuarioAsync(Usuario usuario);      
        Task<OperationResult<string>> ActualizarUsuarioAsync(Usuario usuario);
        Task<OperationResult<Usuario>> GuardarPasswordTemporalAsync(
        string email, string hash, byte[] salt, DateTime fechaExpiracion);
        Task<OperationResult<Role>> AddAsync(Role rol);
    }
}
