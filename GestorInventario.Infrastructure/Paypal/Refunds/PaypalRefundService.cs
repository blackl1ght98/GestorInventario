using System.Globalization;
using GestorInventario.Application.DTOS;
using GestorInventario.Application.ExternalServices.Paypal.API.Requests.POST;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Refunds;
using GestorInventario.Application.Services.Common;
using GestorInventario.Domain.Common;
using GestorInventario.Infrastructure.ExternalServices;
using GestorInventario.Infrastructure.Helpers.Formaters;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.Paypal.Refunds
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
                
                (response.Id!, amountRefunded),"Reembolso procesado correctamente");
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
                    Value = PayPalFormatter.FormatearPayPal(amount),
                    CurrencyCode = currency
                }
            };
        }

        private async Task<PaypalRefundResponse> ExecuteRefundAsync(
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

            return JsonConvert.DeserializeObject<PaypalRefundResponse>(responseBody)
                ?? throw new InvalidOperationException("Respuesta de reembolso inválida");
        }
        // Antes: OrderDetailsResponse
// Ahora: PayPalOrderDetailsDto

        public Task<bool> VerificarReembolsoExistenteAsync(
            PayPalOrderDetailsDto orderDetails,
            decimal monto)
        {
            // Buscar en orderDetails.Refunds si ya hay uno con ese Amount
            var existe = orderDetails.Refunds
                .Any(r => r.Amount == monto); // o con tolerancia decimal si hace falta

            return Task.FromResult(existe);
        }

        public string? ObtenerRefundIdPorMonto(
            PayPalOrderDetailsDto orderDetails,
            decimal monto)
        {
            return orderDetails.Refunds
                .FirstOrDefault(r => r.Amount == monto)
                ?.RefundId;
        }
     
    }
}
