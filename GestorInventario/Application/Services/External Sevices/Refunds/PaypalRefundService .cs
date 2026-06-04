using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.ExternalServices;
using System.Globalization;

namespace GestorInventario.Application.Services.External_Sevices.Refunds
{
    public class PaypalRefundService: PaypalRefundBaseService, IPaypalRefundService
    {
        public PaypalRefundService(
       ILogger<PaypalRefundService> logger,
       IPayPalHttpClient paypal) : base(logger, paypal) { }


        public async Task<OperationResult<(string RefundId, decimal AmountRefunded)>>
            RefundCaptureAsync(
                string captureId,
                decimal amount,
                string currency,
                string? nota = null)
        {
            _logger.LogInformation(
                "Reembolso PayPal -> CaptureId: {CaptureId}, Amount: {Amount} {Currency}",
                captureId, amount, currency);

            if (amount <= 0)
            {
                return OperationResult<(string, decimal)>.Fail(
                    "El importe del reembolso debe ser mayor que cero.");
            }

            var request = BuildRefundRequest(amount, currency, nota ?? "Pedido rembolsado");
            var response = await ExecuteRefundAsync(captureId, request);

            var amountRefunded = decimal.TryParse(
                response.Amount?.Value,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var parsed)
                ? parsed
                : amount;

            return OperationResult<(string, decimal)>.Ok(
                "Reembolso procesado correctamente",
                (response.Id!, amountRefunded));
        }
    }
}
