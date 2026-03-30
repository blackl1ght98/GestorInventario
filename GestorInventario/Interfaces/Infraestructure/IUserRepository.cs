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
        IQueryable<Usuario> ObtenerUsuariosPorRol(int rolId);
        Task<(Usuario?, string)> ObtenerUsuarioConPedido(int id);
        IQueryable<Usuario> ObtenerUsuarios();
        Task ConfirmEmail(ConfirmRegistrationDto confirm);
        Task<OperationResult<string>> ActualizarEmailVerificationTokenAsync(int userId, string token);
        Task<OperationResult<(string temporaryPassword, string token)>> GenerarYGuardarPasswordTemporalAsync(string email);
        Task<List<string>> ObtenerEmailsEmpleadosAsync();
        Task<OperationResult<EntityUser>> ObtenerUsuarioParaEdicionAsync(int id);
    }
}
