using GestorInventario.Application.DTOS.Orders;
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<OperationResult<string>> EliminarPedido(int Id);

        Task<OperationResult<PayPalPaymentDetails>> SincronizarDetallePagoAsync(string id, int pedidoId);
    
        Task<OperationResult<Pedidos>> ConfirmarPagoDelPedidoAsync(PaymentCapturedDto capture, string paymentId);
      
        Task AddInfoTrackingOrder(int pedidoId, string tracking, string carrier);
        Task<OperationResult<DetalleOrdenDto>> GetOrderDetailsAsync(int id);
    }
}
