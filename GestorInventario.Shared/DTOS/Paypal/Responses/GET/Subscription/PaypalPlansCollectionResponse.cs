using GestorInventario.Shared.DTOS.Paypal.Common;
using Newtonsoft.Json;

namespace GestorInventario.Shared.DTOS.Paypal.Responses.GET.Subscription;

public record PaypalPlansCollectionResponse
{
    [JsonProperty("plans")]
    public required List<PaypalPlanResponse> Plans { get; init; }

    [JsonProperty("links")]
    public required List<LinkPaypal> Links { get; init; }
}