using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;
using RefundDto = GestorInventario.Application.DTOS.Paypal.RefundDto;

namespace GestorInventario.Application.Interfaces.Application.Services.Payment
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
