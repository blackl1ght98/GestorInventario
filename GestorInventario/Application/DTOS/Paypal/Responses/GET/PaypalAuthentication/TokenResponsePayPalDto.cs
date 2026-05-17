using Newtonsoft.Json;

namespace GestorInventario.Application.DTOS.Paypal.Responses.GET.PaypalAuthentication
{
    public class TokenResponsePayPalDto
    {
        [JsonProperty("access_token")]
        public required string AccessToken { get; set; }
    }
}
