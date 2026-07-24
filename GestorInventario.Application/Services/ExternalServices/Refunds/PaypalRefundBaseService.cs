using GestorInventario.Application.Services.Common;
using GestorInventario.Interfaces.Application.Services.ExternalServices;
using GestorInventario.Shared.DTOS.Paypal.Requests.POST;
using GestorInventario.Shared.DTOS.Paypal.Responses.POST.Refund;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;


namespace GestorInventario.Application.Services.ExternalServices.Refunds;

public abstract class PaypalRefundBaseService
{
    protected readonly ILogger _logger;
    protected readonly IPayPalHttpClient _paypal;

   
    protected PaypalRefundBaseService(
        ILogger logger,
        IPayPalHttpClient paypal)
    {
        _logger = logger;
        _paypal = paypal;
    }

    protected PaypalRefundRequest BuildRefundRequest(
        decimal amount,
        string currency,
        string nota = "Pedido rembolsado")
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

        return JsonConvert.DeserializeObject<PaypalRefundResponseDto>(responseBody)
            ?? throw new InvalidOperationException("Respuesta de reembolso inválida");
    }
}