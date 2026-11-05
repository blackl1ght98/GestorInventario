using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Infrastructure.Paypal.Refunds
{
    public interface IPaypalRefundService
    {
        Task<OperationResult<(string RefundId, decimal AmountRefunded)>>
            RefundCaptureAsync(
                string captureId,
                decimal amount,
                string currency,
                string? nota = null);

        Task<bool> VerificarReembolsoExistenteAsync(
            PayPalOrderDetailsDto orderDetails,
            decimal monto);
        string? ObtenerRefundIdPorMonto(
            PayPalOrderDetailsDto orderDetails,
            decimal monto);
    }
}