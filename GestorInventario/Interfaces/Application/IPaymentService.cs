
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Interfaces.Application
{
    public interface IPaymentService
    {
        Task<OperationResult<string>> Pagar(string moneda, int userId);
        Task LimpiarPedidoCorruptoUsuarioAsync(int userId);
        OperationResult<PayPalPaymentDetail> ProcesarDetallesRembolsoAsync(OrderDetailsResponse detallespago);
        Task<OperationResult<PayPalPaymentItem>> ProcesarRembolso(PurchaseUnitDetails firstPurchaseUnit, PayPalPaymentDetail detallesSuscripcion, int usuarioActual, RefundFormViewModel form, Pedido obtenerNumeroPedido, string emailCliente);

    }
}
