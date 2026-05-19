using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application.ExternalServices
{
    public interface IPaypalRefundService
    {
        Task<OperationResult<(int pedidoId, string refundId, decimal totalAmount, string orderId)>> RefundSaleAsync(int pedidoId, string currency);
        Task<string> RefundPartialAsync(int pedidoId, string currency, string motivo);
    }
}
