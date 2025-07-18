using GestorInventario.Application.DTOs.Response_paypal.GET;
using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal.POST
{
    public class PaypalRefundResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("note_to_payer")]
        public string NotaParaElCliente { get; set; }
        [JsonProperty("amount")]
        public AmountRefund Amount { get; set; }
    }
    public class AmountRefund
    {
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }

}
