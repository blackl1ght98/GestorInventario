
using Newtonsoft.Json;

namespace GestorInventario.Application.ExternalServices.Paypal.API.Responses.POST.Subscription
{
    public record CreateProductResponse
    {
        [JsonProperty("id")]
        public  string? Id { get; init; }
    }
}
