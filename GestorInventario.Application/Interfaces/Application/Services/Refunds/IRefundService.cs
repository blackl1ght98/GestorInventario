using GestorInventario.Application.DTOS.Rembolso;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.Refunds
{
    public interface IRefundService
    {
        Task<OperationResult<string>> ProcesarRembolsoTotalAsync(
            int pedidoId, string refundId, decimal montoReembolsado);
        Task<OperationResult<(int pedidoId, int detalleId, decimal precioProducto, string motivo)>> RealizarRembolsoParcial(RefundPartialDto request);
    }
}
