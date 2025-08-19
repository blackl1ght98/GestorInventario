using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal
{
   
    public class PaypalErrorResponse
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("debug_id")]
        public string DebugId { get; set; }
        public List<PaypalErrorDetail> Details { get; set; }
        public List<PaypalLink> Links { get; set; }
    }

    public class PaypalErrorDetail
    {
        [JsonProperty("issue")]
        public string Issue { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public class PaypalLink
    {
        [JsonProperty("href")]
        public string Href { get; set; }
        [JsonProperty("rel")]
        public string Rel { get; set; }
    }
}
