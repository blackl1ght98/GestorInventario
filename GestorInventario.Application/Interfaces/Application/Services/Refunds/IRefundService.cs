using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Application.DTOS.Rembolso;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.Refunds
{
    public interface IRefundService
    {
        Task<OperationResult<string>> ProcesarRembolsoTotalAsync(
            int pedidoId, string refundId, decimal montoReembolsado);
        Task<OperationResult<(int pedidoId, int detalleId, decimal precioProducto, string motivo)>> RealizarRembolsoParcial(RefundPartialDto request);
        Task<OperationResult<RembolsoTotal>> EjecutarReembolsoTotalAsync( RefundFullDto request);
        Task<OperationResult<string>> RefundFormAsync(RefundDto form);
    }
}
