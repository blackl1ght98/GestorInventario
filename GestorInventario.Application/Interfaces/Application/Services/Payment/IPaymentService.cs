using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Paypal.BD;
using GestorInventario.Application.ExternalServices.Paypal.API.Responses.GET.Order;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.Payment
{
    public interface IPaymentService
    {
        Task<OperationResult<string>> Pagar(string moneda, int userId);

        Task<OperationResult<PayPalPaymentItem>> ProcesarRembolso(
            PayPalOrderDetailsDto detalles,
            int usuarioActual,
            RefundDto form,
            Pedido obtenerNumeroPedido,
            string emailCliente);       
        Task<OperationResult<string>> ReintentarPago(int pedidoId);
    }
}
