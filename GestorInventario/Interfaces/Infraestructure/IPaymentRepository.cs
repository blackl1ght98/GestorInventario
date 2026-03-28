using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaymentRepository
    {
       
        Task<OperationResult<Pedido>> ObtenerNumeroPedido(RefundFormViewModel form);
        Task<OperationResult<Pedido>> AgregarInfoPedido(int usuarioActual, string? captureId, string? total, string? currency, string? orderId);
        OperationResult<PayPalPaymentDetail> ProcesarDetallesSuscripcion(OrderDetailsResponse detallespago);
        Task<OperationResult<PayPalPaymentItem>> ProcesarRembolso(PurchaseUnitDetails firstPurchaseUnit, PayPalPaymentDetail detallesSuscripcion, int usuarioActual, RefundFormViewModel form, Pedido obtenerNumeroPedido, string emailCliente);
        Task LimpiarPedidoCorruptoUsuarioAsync(int userId);
    }
}
