using Newtonsoft.Json;

namespace GestorInventario.Application.DTOS.Paypal.Responses.POST.Order
{
    /**
      * Representa la respuesta de creación de orden de PayPal.
      * Implementado como record ya que es un dato de solo lectura
      * proveniente de una fuente externa.
      */
    public record PayPalOrderResponse
    {
        [JsonProperty("id")]
        public required string Id { get; init; }

        [JsonProperty("status")]
        public required string Status { get; init; }

        [JsonProperty("links")]
        public required List<PayPalLink> Links { get; init; } = new();
    }

    public record PayPalLink
    {
        [JsonProperty("href")]
        public required string Href { get; init; }

        [JsonProperty("rel")]
        public required string Rel { get; init; }

        [JsonProperty("method")]
        public required string Method { get; init; }
    }

}
