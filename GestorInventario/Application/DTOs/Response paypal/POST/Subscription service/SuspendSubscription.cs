using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal
{
    public class SuspendSubscription
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("status")]
        public required string Status { get; set; }
    }
}
