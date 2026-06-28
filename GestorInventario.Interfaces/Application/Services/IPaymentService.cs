using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Order;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Interfaces.Application.Services
{
    public interface IPaymentService
    {
        Task<OperationResult<string>> Pagar(string moneda, int userId);

        Task<OperationResult<PayPalPaymentItem>> ProcesarRembolso(PurchaseUnitDetails firstPurchaseUnit, PayPalPaymentDetail detallesSuscripcion, int usuarioActual, RefundDto form, Pedido obtenerNumeroPedido, string emailCliente);
        Task<OperationResult<string>> ReintentarPago(int pedidoId);
    }
}
