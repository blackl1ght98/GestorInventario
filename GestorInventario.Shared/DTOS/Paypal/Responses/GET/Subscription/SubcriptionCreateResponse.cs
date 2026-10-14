using GestorInventario.Shared.DTOS.Paypal.Common;
using Newtonsoft.Json;

namespace GestorInventario.Shared.DTOS.Paypal.Responses.GET.Subscription;

public class SubcriptionCreateResponse
{
    [JsonProperty("links")]
    public  List<LinkPaypal>? Links { get; set; }
}