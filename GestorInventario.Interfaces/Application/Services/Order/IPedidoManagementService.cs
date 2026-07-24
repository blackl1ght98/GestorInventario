using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Interfaces.Application.Services.Order
{
    public interface IPedidoManagementService
    {
        Task<OperationResult<string>> EliminarPedido(int Id);

        Task<OperationResult<PayPalPaymentDetail>> SincronizarDetallePagoAsync(string id, int pedidoId);
    
        Task<OperationResult<Pedido>> ConfirmarPagoDelPedidoAsync(int usuarioActual, string captureId, decimal total, string? currency, string orderId);
        Task ProcesarRembolsoAsync(int pedidoId, string status, string refundId);

        Task RegistrarReembolsoParcialAsync(int pedidoId, int detalleId, string motivo, decimal montoRembolsado, string currency, string refundId);
        Task AddInfoTrackingOrder(int pedidoId, string tracking, string url, string carrier);
    }
}
