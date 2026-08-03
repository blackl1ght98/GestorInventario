using Newtonsoft.Json;

namespace GestorInventario.Shared.DTOS.Paypal.Responses.GET.Subscription
{
    public record PaypalProductListResponseDto
    {
        [JsonProperty("total_items")]
        public int TotalItems { get; init; }
        [JsonProperty("total_pages")]
        public int TotalPages { get; init; }
        [JsonProperty("products")]
        public List<Products> Products { get; init; } = new List<Products>();
        public List<Links> Links { get; init; } = new List<Links>();
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
        public required List<Links> Links { get; init; }
    }
    public record Links
    {
        [JsonProperty("href")]
        public required string Href { get; init; }

        [JsonProperty("rel")]
        public required string Rel { get; init; }

        [JsonProperty("method")]
        public required string Method { get; init; }

    }
}
