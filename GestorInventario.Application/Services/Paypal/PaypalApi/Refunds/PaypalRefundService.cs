using GestorInventario.Application.Services.Common;
using GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi;
using GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Refunds;
using GestorInventario.Shared.DTOS.Paypal.Requests.POST;
using GestorInventario.Shared.DTOS.Paypal.Responses.POST.Refund;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Globalization;

namespace GestorInventario.Application.Services.Paypal.PaypalApi.Refunds
{
    public class PaypalRefundService : IPaypalRefundService
    {
        private readonly ILogger<PaypalRefundService> _logger;
        private readonly IPayPalHttpClient _paypal;

        public PaypalRefundService(
            ILogger<PaypalRefundService> logger,
            IPayPalHttpClient paypal)
        {
            _logger = logger;
            _paypal = paypal;
        }

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

        private PaypalRefundRequest BuildRefundRequest(
            decimal amount,
            string currency,
            string nota)
        {
            return new PaypalRefundRequest
            {
                NotaParaElCliente = nota,
                Amount = new AmountRefundRequest
                {
                    Value = CalculadoraFiscal.FormatearPayPal(amount),
                    CurrencyCode = currency
                }
            };
        }

        private async Task<PaypalRefundResponseDto> ExecuteRefundAsync(
            string captureId,
            PaypalRefundRequest request)
        {
            var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                HttpMethod.Post,
                $"v2/payments/captures/{captureId}/refund",
                request,
                async resp =>
                {
                    var errBody = await resp.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Error PayPal: {resp.StatusCode} - {errBody}");
                });

            return JsonConvert.DeserializeObject<PaypalRefundResponseDto>(responseBody)
                ?? throw new InvalidOperationException("Respuesta de reembolso inválida");
        }
    }
}
