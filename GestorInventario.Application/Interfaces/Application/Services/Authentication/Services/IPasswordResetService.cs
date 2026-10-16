using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.Authentication.Services
{
    public interface IPasswordResetService
    {
        Task<OperationResult<string>> GenerarPasswordTemporalAsync(string email);
    }
}
