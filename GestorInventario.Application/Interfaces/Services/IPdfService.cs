using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Services
{
    public interface IPdfService
    {
        Task<OperationResult<byte[]>> GenerarFacturaPagoEjecutadoAsync(string pagoId);
    }
}
