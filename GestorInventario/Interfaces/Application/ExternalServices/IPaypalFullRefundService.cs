using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application.ExternalServices
{
    public interface IPaypalFullRefundService
    {
        Task<OperationResult<(int pedidoId, string refundId, decimal totalAmount, string orderId)>> RefundSaleAsync(int pedidoId, string currency);
    }
}