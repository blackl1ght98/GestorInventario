using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application.ExternalServices
{
    public interface IPaypalPartialRefundService
    {
        Task<OperationResult<(int pedidoId, int detalleId, string refundId, decimal montoRembolsado, string motivo, string estadoVenta, decimal precioProducto)>> RefundPartialAsync(int pedidoId, string currency, string motivo);
    }
}