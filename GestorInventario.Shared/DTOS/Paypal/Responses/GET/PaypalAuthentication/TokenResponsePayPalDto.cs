using Newtonsoft.Json;

namespace GestorInventario.Shared.DTOS.Paypal.Responses.GET.PaypalAuthentication
{
    public record TokenResponsePayPalDto
    {
        [JsonProperty("access_token")]
        public required string AccessToken { get; init; }
    }
}
