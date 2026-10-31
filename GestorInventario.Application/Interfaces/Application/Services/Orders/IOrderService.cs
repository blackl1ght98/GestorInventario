using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Application.DTOS.Pedidos;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.Orders
{
    public interface IOrderService
    {
        Task<OperationResult<string>> EliminarPedido(int Id);

        Task<OperationResult<PayPalPaymentDetail>> SincronizarDetallePagoAsync(string id, int pedidoId);
    
        Task<OperationResult<Pedido>> ConfirmarPagoDelPedidoAsync(PaymentCapturedDto capture, string paymentId);
      
        Task AddInfoTrackingOrder(int pedidoId, string tracking, string carrier);
        Task<OperationResult<DetalleOrdenDto>> GetOrderDetailsAsync(int id);
    }
}
