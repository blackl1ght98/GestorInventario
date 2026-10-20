
using GestorInventario.Infrastructure.ExternalServices.Common;
using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.ExternalServices;

public class SubcriptionCreateResponse
{
    [JsonProperty("links")]
    public  List<LinkPaypal>? Links { get; set; }
}