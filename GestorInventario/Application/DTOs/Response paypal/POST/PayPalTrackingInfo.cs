using GestorInventario.enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GestorInventario.Application.DTOs.Response_paypal.POST
{
    public class PayPalTrackingInfo
    {
        [JsonProperty("capture_id")]
        public string CaptureId { get; set; }
        [JsonProperty("tracking_number")]
        public string TrackingNumber { get; set; }
        [JsonProperty("carrier")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Carrier Carrier { get; set; }
        [JsonProperty("notify_payer")]
        public bool NotifyPayer { get; set; }
        [JsonProperty("items")]
        public List<TrackingItems> Items { get; set; }

    }
    public class TrackingItems
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("sku")]
        public string Sku { get; set; }
        [JsonProperty("quantity")]
        public int Quantity { get; set; }
        [JsonProperty("upc")]
        public Upc Upc { get; set; }
        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
      
    }
    public class Upc
    {
        [JsonProperty("type")]
        public string Type { get; set; } // Ejemplo: "UPC-A", "UPC-B", "EAN-13", "ISBN"
        [JsonProperty("code")]
        public string Code { get; set; } // Ejemplo: "upc001"
    }
}
