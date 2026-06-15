using Newtonsoft.Json;

namespace GestorInventario.Application.DTOS.Paypal.Requests.POST
{
    public record PaypalRefundRequest
    {

        [JsonProperty("note_to_payer")]
        public string? NotaParaElCliente { get; init; }
        [JsonProperty("amount")]
        public AmountRefundRequest? Amount { get; init; }
    }
    public record AmountRefundRequest
    {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; init; }
        [JsonProperty("value")]
        public required string Value { get; init; }
    }
}
