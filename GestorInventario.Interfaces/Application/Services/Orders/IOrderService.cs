using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Interfaces.Application.Services.Orders
{
    public interface IOrderService
    {
        Task<OperationResult<string>> EliminarPedido(int Id);

        Task<OperationResult<PayPalPaymentDetail>> SincronizarDetallePagoAsync(string id, int pedidoId);
    
        Task<OperationResult<Pedido>> ConfirmarPagoDelPedidoAsync(int usuarioActual, string captureId, decimal total, string? currency, string orderId);
      
        Task AddInfoTrackingOrder(int pedidoId, string tracking, string carrier);
    }
}
