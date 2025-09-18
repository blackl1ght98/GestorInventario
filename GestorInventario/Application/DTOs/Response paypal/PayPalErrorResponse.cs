using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal
{
   
    public class PaypalErrorResponse
    {
        [JsonProperty("name")]
        public required string Name { get; set; }
        [JsonProperty("message")]
        public required string Message { get; set; }
        [JsonProperty("debug_id")]
        public  required string DebugId { get; set; }
        public required List<PaypalErrorDetail> Details { get; set; }
        public List<PaypalLink>? Links { get; set; }
    }

    public class PaypalErrorDetail
    {
        [JsonProperty("issue")]
        public required string Issue { get; set; }
        [JsonProperty("description")]
        public required string Description { get; set; }
    }

    public class PaypalLink
    {
        [JsonProperty("href")]
        public string? Href { get; set; }
        [JsonProperty("rel")]
        public string? Rel { get; set; }
    }
}
