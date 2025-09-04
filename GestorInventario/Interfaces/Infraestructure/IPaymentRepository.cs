using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Domain.Models;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaymentRepository
    {
        Task<string?> ObtenerEmailUsuarioAsync(int usuarioId);
        Task<(Pedido?, string)> ObtenerNumeroPedido(RefundForm form);
        Task<(Pedido?, string)> AgregarInfoPedido(int usuarioActual, string? captureId, string? total, string? currency, string? orderId);
        decimal? ConvertToDecimal(object value);
        int? ConvertToInt(object value);
        DateTime? ConvertToDateTime(object value);
        PayPalPaymentDetail ProcesarDetallesSuscripcion(CheckoutDetails detallespago);
        Task<(PayPalPaymentItem?, string)> ProcesarRembolso(PurchaseUnitsBse firstPurchaseUnit, PayPalPaymentDetail detallesSuscripcion, int usuarioActual, RefundForm form, Pedido obtenerNumeroPedido, string emailCliente);

    }
}
