using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.ExternalServices
{
    public record TokenResponsePayPal
    {
        [JsonProperty("access_token")]
        public required string AccessToken { get; init; }
    }
}
