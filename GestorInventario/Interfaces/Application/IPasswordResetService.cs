using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application
{
    public interface IPasswordResetService
    {
        Task<OperationResult<(string temporaryPassword, string token)>> GenerarPasswordTemporalAsync(string email);
    }
}
