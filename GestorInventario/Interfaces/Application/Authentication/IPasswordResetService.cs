

using GestorInventario.Utilities;

namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface IPasswordResetService
    {
        Task<OperationResult<string>> GenerarPasswordTemporalAsync(string email);
    }
}
