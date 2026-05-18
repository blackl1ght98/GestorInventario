using Newtonsoft.Json;

namespace GestorInventario.Application.DTOS.Paypal.Responses.POST.Subscription
{
    public class ActivateSubscriptionResponseDto
    {
        [JsonProperty("id")]
        public string? Id { get; set; }
        [JsonProperty("status")]
        public required string Status { get; set; }
    }
}
