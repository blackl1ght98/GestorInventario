using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Refunds
{
    public interface IPaypalRefundService
    {
        Task<OperationResult<(string RefundId, decimal AmountRefunded)>>
            RefundCaptureAsync(
                string captureId,
                decimal amount,
                string currency,
                string? nota = null);
    }
}