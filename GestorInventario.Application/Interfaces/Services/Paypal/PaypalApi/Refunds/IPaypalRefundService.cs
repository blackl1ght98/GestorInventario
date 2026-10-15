using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Services.Paypal.PaypalApi.Refunds
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