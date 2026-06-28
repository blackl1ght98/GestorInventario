
using GestorInventario.Application.DTOs.Paypal.Responses.GET.Order;
using GestorInventario.Application.DTOS;
using GestorInventario.Domain.Models;

using GestorInventario.Utilities;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IPaymentService
    {
        Task<OperationResult<string>> Pagar(string moneda, int userId);

        Task<OperationResult<PayPalPaymentItem>> ProcesarRembolso(PurchaseUnitDetails firstPurchaseUnit, PayPalPaymentDetail detallesSuscripcion, int usuarioActual, RefundDto form, Pedido obtenerNumeroPedido, string emailCliente);
        Task<OperationResult<string>> ReintentarPago(int pedidoId);
    }
}
