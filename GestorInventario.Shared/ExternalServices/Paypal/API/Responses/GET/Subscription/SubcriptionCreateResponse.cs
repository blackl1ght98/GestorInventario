using GestorInventario.Shared.ExternalServices.Paypal.API.Common;
using Newtonsoft.Json;

namespace GestorInventario.Shared.ExternalServices.Paypal.API.Responses.GET.Subscription;

public class SubcriptionCreateResponse
{
    [JsonProperty("links")]
    public  List<LinkPaypal>? Links { get; set; }
}