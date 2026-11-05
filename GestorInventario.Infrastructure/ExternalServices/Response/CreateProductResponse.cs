
using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.ExternalServices.Response
{
    public record CreateProductResponse
    {
        [JsonProperty("id")]
        public  string? Id { get; init; }
    }
}
