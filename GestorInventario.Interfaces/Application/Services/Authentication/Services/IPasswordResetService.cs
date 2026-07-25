using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Application.Services.Authentication.Services
{
    public interface IPasswordResetService
    {
        Task<OperationResult<string>> GenerarPasswordTemporalAsync(string email);
    }
}
