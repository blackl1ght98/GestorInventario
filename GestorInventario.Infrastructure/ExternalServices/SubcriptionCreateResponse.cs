using GestorInventario.Application.ExternalServices.Paypal.API.Common;
using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.ExternalServices;

public class SubcriptionCreateResponse
{
    [JsonProperty("links")]
    public  List<LinkPaypal>? Links { get; set; }
}