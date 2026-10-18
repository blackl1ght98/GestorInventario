using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Infrastructure.ExternalServices.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GestorInventario.Infrastructure.ExternalServices
{
    public record PayPalTrackingInfoRequest
    {
        [JsonProperty("capture_id")]
        public required string CaptureId { get; init; }
        [JsonProperty("tracking_number")]
        public required string TrackingNumber { get; init; }
        [JsonProperty("carrier")]
        [JsonConverter(typeof(StringEnumConverter))]
        public required Carrier Carrier { get; init; }
        [JsonProperty("notify_payer")]
        public required bool NotifyPayer { get; init; }
        [JsonProperty("items")]
        public required List<TrackingItems> Items { get; init; }

    }
    public record TrackingItems
    {
        [JsonProperty("name")]
        public required string Name { get; init; }
        [JsonProperty("sku")]
        public required string Sku { get; init; }
        [JsonProperty("quantity")]
        public required int Quantity { get; init; }
        [JsonProperty("upc")]
        public required Upc Upc { get; init; }
        [JsonProperty("image_url")]
        public required string ImageUrl { get; init; }
        [JsonProperty("url")]
        public required string Url { get; init; }
      
    }
   
}
