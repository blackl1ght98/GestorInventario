
using Newtonsoft.Json;

namespace GestorInventario.Shared.DTOS.Paypal.Responses.POST.Subscription
{
    public record CreateProductResponse
    {
        [JsonProperty("id")]
        public  string? Id { get; init; }
    }
}
