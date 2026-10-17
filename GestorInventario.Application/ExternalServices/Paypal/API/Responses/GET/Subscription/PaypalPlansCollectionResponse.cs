using GestorInventario.Application.ExternalServices.Paypal.API.Common;

using Newtonsoft.Json;

namespace GestorInventario.Application.ExternalServices.Paypal.API.Responses.GET.Subscription;

public record PaypalPlansCollectionResponse
{
    [JsonProperty("plans")]
    public required List<PaypalPlanResponse> Plans { get; init; }

    [JsonProperty("links")]
    public required List<LinkPaypal> Links { get; init; }
}