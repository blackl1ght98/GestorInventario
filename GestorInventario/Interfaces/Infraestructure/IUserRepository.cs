using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IUserRepository
    {
       
        Task<OperationResult<Usuario>> ObtenerUsuarioPorId(int id);
        Task<List<Usuario>> ObtenerUsuariosAsync();
        Task<(Usuario?, string)> ObtenerUsuarioConPedido(int id);
        Task ConfirmEmail(ConfirmRegistrationDto confirm);
        Task<OperationResult<string>> ActualizarEmailVerificationTokenAsync(int userId, string token);
        Task<List<string>> ObtenerEmailsEmpleadosAsync();
        Task<OperationResult<EntityUser>> ObtenerUsuarioParaEdicionAsync(int id);
        Task<OperationResult<Usuario>> AgregarUsuarioAsync(Usuario usuario);
        Task<bool> ExisteEmailAsync(string email);
        Task<OperationResult<string>> ActualizarUsuarioAsync(EntityUser usuarioDominio);
        Task<OperationResult<Usuario>> GuardarPasswordTemporalAsync(
        string email, string hash, byte[] salt, DateTime fechaExpiracion);
        Task<OperationResult<Usuario>> ObtenerUsuarioConProveedoresYPedidosAsync(int id);

    }
}
