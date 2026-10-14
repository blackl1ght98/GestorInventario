using GestorInventario.Shared.ExternalServices.Paypal.API.Common;
using Newtonsoft.Json;

namespace GestorInventario.Shared.ExternalServices.Paypal.API.Responses.GET.Subscription
{
    public record PaypalProductCollectionResponse
    {
        [JsonProperty("total_items")]
        public int TotalItems { get; init; }
        [JsonProperty("total_pages")]
        public int TotalPages { get; init; }
        [JsonProperty("products")]
        public List<Products> Products { get; init; } = new List<Products>();
        public List<LinkPaypal> Links { get; init; } = new List<LinkPaypal>();
    }
    public record Products
    {
        [JsonProperty("id")]
        public required string Id { get; init; }
        [JsonProperty("name")]
        public required string Name { get; init; }
        [JsonProperty("description")]
        public required string Description { get; init; }
        [JsonProperty("create_time")]
        public required string CreateTime { get; init; }
        [JsonProperty("links")]
        public required List<LinkPaypal> Links { get; init; }
    }
  
}
