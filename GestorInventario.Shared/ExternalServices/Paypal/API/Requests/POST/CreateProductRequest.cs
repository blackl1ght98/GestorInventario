using Newtonsoft.Json;

namespace GestorInventario.Shared.ExternalServices.Paypal.API.Requests.POST
{
    public record CreateProductRequest
    {
        [JsonProperty("name")]
        public required string Nombre { get; init; }

        [JsonProperty("description")]
        public required string Description { get; init; }

        [JsonProperty("type")]
        public required string Type { get; init; }

        [JsonProperty("category")]
        public required string Category { get; init; }

        [JsonProperty("image_url")]
        public string? Imagen { get; init; }
    }
}
