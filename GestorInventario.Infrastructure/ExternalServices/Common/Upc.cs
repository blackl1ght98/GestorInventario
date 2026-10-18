using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.ExternalServices.Common;

public record Upc
{
    [JsonProperty("type")]
    public required string Type { get; init; }

    [JsonProperty("code")]
    public required string Code { get; init; }
}