using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal.GET
{
    public class TokenResponsePayPal
    {
        [JsonProperty("access_token")]
        public required string AccessToken { get; set; }
    }
}
