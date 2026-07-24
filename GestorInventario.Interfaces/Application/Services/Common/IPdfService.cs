using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Application.Services.Common
{
    public interface IPdfService
    {
        Task<OperationResult<byte[]>> GenerarFacturaPagoEjecutadoAsync(string pagoId);
    }
}
