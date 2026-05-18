using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

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
        Task<List<string>> ObtenerEmailsEmpleadosAsync();
        //Operaciones
        Task ConfirmEmail(ConfirmRegistrationDto confirm);
        Task<OperationResult<string>> ActualizarEmailVerificationTokenAsync(int userId, string token); 
        Task<OperationResult<Usuario>> AgregarUsuarioAsync(Usuario usuario);      
        Task<OperationResult<string>> ActualizarUsuarioAsync(Usuario usuario);
        Task<OperationResult<Usuario>> GuardarPasswordTemporalAsync(
        string email, string hash, byte[] salt, DateTime fechaExpiracion);
    }
}
