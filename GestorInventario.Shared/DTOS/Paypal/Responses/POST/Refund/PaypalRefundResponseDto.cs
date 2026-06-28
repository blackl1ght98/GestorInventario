using Newtonsoft.Json;

namespace GestorInventario.Shared.DTOS.Paypal.Responses.POST.Refund
{
    public record PaypalRefundResponseDto
    {
        [JsonProperty("id")]
        public  string? Id { get; init; }
        [JsonProperty("note_to_payer")]
        public  string? NotaParaElCliente { get; init; }
        [JsonProperty("amount")]
        public   AmountRefund? Amount { get; init; }
    }
    public record AmountRefund
    {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; init; }
        [JsonProperty("value")]
        public required string Value { get; init; }
    }

}
