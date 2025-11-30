using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaymentRepository
    {
        Task<string?> ObtenerEmailUsuarioAsync(int usuarioId);
        Task<OperationResult<Pedido>> ObtenerNumeroPedido(RefundFormViewModel form);
        Task<OperationResult<Pedido>> AgregarInfoPedido(int usuarioActual, string? captureId, string? total, string? currency, string? orderId);
        decimal? ConvertToDecimal(object value);
        int? ConvertToInt(object value);
       
        PayPalPaymentDetail ProcesarDetallesSuscripcion(CheckoutDetailsDto detallespago);
        Task<OperationResult<PayPalPaymentItem>> ProcesarRembolso(PurchaseUnitsBse firstPurchaseUnit, PayPalPaymentDetail detallesSuscripcion, int usuarioActual, RefundFormViewModel form, Pedido obtenerNumeroPedido, string emailCliente);

    }
}
