using GestorInventario.Domain.enums.Usuario;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.User;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Infraestructure.Repositories
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
        Task ConfirmEmail(ConfirmRegistrationDto confirm);
        Task<OperationResult<string>> ActualizarEmailVerificationTokenAsync(int userId, string token); 
        Task<OperationResult<Usuario>> AgregarUsuarioAsync(Usuario usuario);      
        Task<OperationResult<string>> ActualizarUsuarioAsync(Usuario usuario);
        Task<OperationResult<Usuario>> GuardarPasswordTemporalAsync(
        string email, string hash, byte[] salt, DateTime fechaExpiracion);
        Task<OperationResult<Role>> AddAsync(Role rol);
    }
}
