using Newtonsoft.Json;

namespace GestorInventario.Application.DTOS.Paypal.Responses.POST.Refund
{
    public class PaypalRefundResponseDto
    {
        [JsonProperty("id")]
        public  string? Id { get; set; }
        [JsonProperty("note_to_payer")]
        public  string? NotaParaElCliente { get; set; }
        [JsonProperty("amount")]
        public   AmountRefund? Amount { get; set; }
    }
    public class AmountRefund
    {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; set; }
        [JsonProperty("value")]
        public required string Value { get; set; }
    }

}
