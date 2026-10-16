using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.PDFService
{
    public interface IPdfService
    {
        Task<OperationResult<byte[]>> GenerarFacturaPagoEjecutadoAsync(string pagoId);
    }
}
