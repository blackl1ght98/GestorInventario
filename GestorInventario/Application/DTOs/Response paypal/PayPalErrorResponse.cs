using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal
{
    public class PayPalErrorResponse
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("debug_id")]
        public string DebugId { get; set; }
    }
}
