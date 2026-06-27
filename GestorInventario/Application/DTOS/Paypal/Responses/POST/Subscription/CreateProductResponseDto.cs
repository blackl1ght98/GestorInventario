using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace GestorInventario.Application.DTOS.Paypal.Responses.POST.Subscription
{
    public record CreateProductResponseDto
    {
        [JsonProperty("id")]
        public  string? Id { get; init; }
    }
}
