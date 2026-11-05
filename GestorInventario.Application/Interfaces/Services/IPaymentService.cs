using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Services
{
    public interface IPaymentService
    {
        Task<OperationResult<string>> Pagar(string moneda, int userId);

        Task<OperationResult<PayPalPaymentItem>> ProcesarRembolso(
            PayPalOrderDetailsDto detalles,
            int usuarioActual,
            RefundDto form,
            int pedidoId,
            string emailCliente);      
        Task<OperationResult<string>> ReintentarPago(int pedidoId);
    }
}
