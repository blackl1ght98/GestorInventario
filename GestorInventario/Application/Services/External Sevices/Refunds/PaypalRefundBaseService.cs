using GestorInventario.Application.DTOS.Paypal.Requests.POST;
using GestorInventario.Application.DTOS.Paypal.Responses.POST.Refund;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Newtonsoft.Json;
using System.Globalization;

namespace GestorInventario.Application.Services.External_Sevices.Refunds
{
    public abstract class PaypalRefundBaseService
    {
        protected readonly ILogger _logger;
        protected readonly IPayPalHttpClient _paypal;
        protected readonly IPedidoRepository _pedidoRepository;

        protected PaypalRefundBaseService(
            ILogger logger,
            IPayPalHttpClient paypal,
            IPedidoRepository pedidoRepository)
        {
            _logger = logger;
            _paypal = paypal;
            _pedidoRepository = pedidoRepository;
        }

        protected PaypalRefundRequest BuildRefundRequest(decimal amount, string currency, string nota = "Pedido rembolsado")
        {
            return new PaypalRefundRequest
            {
                NotaParaElCliente = nota,
                Amount = new AmountRefundRequest
                {
                    Value = amount.ToString("F2", CultureInfo.InvariantCulture),
                    CurrencyCode = currency
                }
            };
        }

        protected async Task<PaypalRefundResponseDto> ExecuteRefundAsync(
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

            return JsonConvert.DeserializeObject <PaypalRefundResponseDto > (responseBody)
                ?? throw new InvalidOperationException("Respuesta de reembolso inválida");
        }
    }
}
