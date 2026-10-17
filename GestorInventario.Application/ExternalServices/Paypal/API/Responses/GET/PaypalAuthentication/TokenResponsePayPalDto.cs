using Newtonsoft.Json;

namespace GestorInventario.Application.ExternalServices.Paypal.API.Responses.GET.PaypalAuthentication
{
    public record TokenResponsePayPalDto
    {
        [JsonProperty("access_token")]
        public required string AccessToken { get; init; }
    }
}
