using Newtonsoft.Json;

namespace GestorInventario.Application.Classes
{
    public class PayPalOrderResponseDto
    {
        [JsonProperty("id")]
        public required string Id { get; set; }
        [JsonProperty("status")]
        public required string Status { get; set; }
        [JsonProperty("links")]
        public required List<PayPalLink> Links { get; set; }
    }

    public class PayPalLink
    {
        [JsonProperty("href")]
        public required string Href { get; set; }
        [JsonProperty("rel")]
        public required string Rel { get; set; }
        [JsonProperty("method")]
        public required string Method { get; set; }
    }

}
