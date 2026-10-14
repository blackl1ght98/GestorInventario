using Newtonsoft.Json;

namespace GestorInventario.Shared.ExternalServices.Paypal.API.Common;

public class LinkPaypal
{
    [JsonProperty("href")]
    public required string Href { get; init; }

    [JsonProperty("rel")]
    public required string Rel { get; init; }

    [JsonProperty("method")]
    public required string Method { get; init; }

    [JsonProperty("encType")]
    public required string EncType { get; init; }
}