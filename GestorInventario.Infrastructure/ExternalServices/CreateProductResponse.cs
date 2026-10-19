
using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.ExternalServices
{
    public record CreateProductResponse
    {
        [JsonProperty("id")]
        public  string? Id { get; init; }
    }
}
