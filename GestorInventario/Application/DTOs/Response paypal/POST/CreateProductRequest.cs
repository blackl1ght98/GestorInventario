using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal.POST
{
    public class CreateProductRequest
    {
        [JsonProperty("name")]
        public required string Nombre { get; set; }

        [JsonProperty("description")]
        public required string Description { get; set; }

        [JsonProperty("type")]
        public required string Type { get; set; }

        [JsonProperty("category")]
        public required string Category { get; set; }

        [JsonProperty("image_url")]
        public string? Imagen { get; set; }
    }
}
