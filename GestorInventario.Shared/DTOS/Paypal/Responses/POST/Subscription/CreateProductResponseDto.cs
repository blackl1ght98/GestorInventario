using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace GestorInventario.Shared.DTOS.Paypal.Responses.POST.Subscription
{
    public record CreateProductResponseDto
    {
        [JsonProperty("id")]
        public  string? Id { get; init; }
    }
}
