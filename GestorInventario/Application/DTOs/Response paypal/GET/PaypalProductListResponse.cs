using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal.GET
{
    public class PaypalProductListResponse
    {
        [JsonProperty("total_items")]
        public int TotalItems { get; set; }
        [JsonProperty("total_pages")]
        public int TotalPages { get; set; }
        [JsonProperty("products")]
        public List<Products> Products { get; set; } = new List<Products>();
        public List<Links> Links { get; set; } = new List<Links>();
    }
    public class Products
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("create_time")]
        public string CreateTime { get; set; }
        [JsonProperty("links")]
        public List<Links> Links { get; set; }
    }
    public class Links
    {
        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("rel")]
        public string Rel { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

    }
}
