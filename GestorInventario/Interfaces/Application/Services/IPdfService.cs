
using GestorInventario.Utilities;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IPdfService
    {
        Task<OperationResult<byte[]>> GenerarFacturaPagoEjecutadoAsync(string pagoId);
    }
}
