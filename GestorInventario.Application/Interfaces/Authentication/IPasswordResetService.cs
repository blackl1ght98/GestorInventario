using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Authentication
{
    public interface IPasswordResetService
    {
        Task<OperationResult<string>> GenerarPasswordTemporalAsync(string email);
    }
}
