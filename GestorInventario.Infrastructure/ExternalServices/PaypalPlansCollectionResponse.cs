
using GestorInventario.Infrastructure.ExternalServices.Common;
using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.ExternalServices;

public record PaypalPlansCollectionResponse
{
    [JsonProperty("plans")]
    public required List<PaypalPlanResponse> Plans { get; init; }

    [JsonProperty("links")]
    public required List<LinkPaypal> Links { get; init; }
}