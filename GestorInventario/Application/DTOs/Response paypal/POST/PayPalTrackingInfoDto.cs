using GestorInventario.enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GestorInventario.Application.DTOs.Response_paypal.POST
{
    public class PayPalTrackingInfoDto
    {
        [JsonProperty("capture_id")]
        public required string CaptureId { get; set; }
        [JsonProperty("tracking_number")]
        public required string TrackingNumber { get; set; }
        [JsonProperty("carrier")]
        [JsonConverter(typeof(StringEnumConverter))]
        public required Carrier Carrier { get; set; }
        [JsonProperty("notify_payer")]
        public required bool NotifyPayer { get; set; }
        [JsonProperty("items")]
        public required List<TrackingItems> Items { get; set; }

    }
    public class TrackingItems
    {
        [JsonProperty("name")]
        public required string Name { get; set; }
        [JsonProperty("sku")]
        public required string Sku { get; set; }
        [JsonProperty("quantity")]
        public required int Quantity { get; set; }
        [JsonProperty("upc")]
        public required Upc Upc { get; set; }
        [JsonProperty("image_url")]
        public required string ImageUrl { get; set; }
        [JsonProperty("url")]
        public required string Url { get; set; }
      
    }
    public class Upc
    {
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BarcodeType Type { get; set; } // Ejemplo: "UPC-A", "UPC-B", "EAN-13", "ISBN"
        [JsonProperty("code")]
        public required string Code { get; set; } // Ejemplo: "upc001"
    }
}
