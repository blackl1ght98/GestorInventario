using Newtonsoft.Json;

namespace GestorInventario.Application.DTOS.Paypal.Requests.POST
{
    public class PaypalRefundRequest
    {

        [JsonProperty("note_to_payer")]
        public string? NotaParaElCliente { get; set; }
        [JsonProperty("amount")]
        public AmountRefundRequest? Amount { get; set; }
    }
    public class AmountRefundRequest
    {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; set; }
        [JsonProperty("value")]
        public required string Value { get; set; }
    }
}
