using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GestorInventario.Application.DTOs
{
    public class PaypalPlanResponse
    {
        [JsonProperty("id")]
        [Required(ErrorMessage = "El ID del plan es requerido.")]
        public string Id { get; set; }

        [JsonProperty("product_id")]
        [Required(ErrorMessage = "El ID del producto es requerido.")]
        public string ProductId { get; set; }

        [JsonProperty("name")]
        [Required(ErrorMessage = "El nombre del plan es requerido.")]
        public string Name { get; set; }

        [JsonProperty("status")]
        [Required(ErrorMessage = "El estado del plan es requerido.")]
        public string Status { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("usage_type")]
        public string UsageType { get; set; }

        [JsonProperty("billing_cycles")]
        [Required(ErrorMessage = "Los ciclos de facturación son requeridos.")]
        public List<BillingCycle> BillingCycles { get; set; }

        [JsonProperty("payment_preferences")]
        [Required(ErrorMessage = "Las preferencias de pago son requeridas.")]
        public PaymentPreferences PaymentPreferences { get; set; }

        [JsonProperty("taxes")]
        public Taxes Taxes { get; set; }

        [JsonProperty("quantity_supported")]
        public bool QuantitySupported { get; set; }

        [JsonProperty("create_time")]
        public DateTime CreateTime { get; set; }

        [JsonProperty("update_time")]
        public DateTime UpdateTime { get; set; }

        [JsonProperty("links")]
        public List<Link> Links { get; set; }
    }

    // DTO para los ciclos de facturación
    public class BillingCycle
    {
        [JsonProperty("pricing_scheme")]
        [Required(ErrorMessage = "El esquema de precios es requerido.")]
        public PricingScheme PricingScheme { get; set; }

        [JsonProperty("frequency")]
        [Required(ErrorMessage = "La frecuencia es requerida.")]
        public Frequency Frequency { get; set; }

        [JsonProperty("tenure_type")]
        [Required(ErrorMessage = "El tipo de tenencia es requerido.")]
        public string TenureType { get; set; }

        [JsonProperty("sequence")]
        public int Sequence { get; set; }

        [JsonProperty("total_cycles")]
        public int TotalCycles { get; set; }
    }

    // DTO para el esquema de precios
    public class PricingScheme
    {
        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("fixed_price")]
        [Required(ErrorMessage = "El precio fijo es requerido.")]
        public Money FixedPrice { get; set; }

        [JsonProperty("create_time")]
        public DateTime CreateTime { get; set; }

        [JsonProperty("update_time")]
        public DateTime UpdateTime { get; set; }
    }

    // DTO para el monto
    public class Money
    {
        [JsonProperty("currency_code")]
        [Required(ErrorMessage = "El código de moneda es requerido.")]
        public string CurrencyCode { get; set; }

        [JsonProperty("value")]
        [Required(ErrorMessage = "El valor del monto es requerido.")]
        public string Value { get; set; }
    }

    // DTO para la frecuencia
    public class Frequency
    {
        [JsonProperty("interval_unit")]
        [Required(ErrorMessage = "La unidad de intervalo es requerida.")]
        public string IntervalUnit { get; set; }

        [JsonProperty("interval_count")]
        public int IntervalCount { get; set; }
    }

    // DTO para las preferencias de pago
    public class PaymentPreferences
    {
        [JsonProperty("service_type")]
        public string ServiceType { get; set; }

        [JsonProperty("auto_bill_outstanding")]
        public bool AutoBillOutstanding { get; set; }

        [JsonProperty("setup_fee")]
        public Money SetupFee { get; set; }

        [JsonProperty("setup_fee_failure_action")]
        public string SetupFeeFailureAction { get; set; }

        [JsonProperty("payment_failure_threshold")]
        public int PaymentFailureThreshold { get; set; }
    }

    // DTO para los impuestos
    public class Taxes
    {
        [JsonProperty("percentage")]
        public string Percentage { get; set; }

        [JsonProperty("inclusive")]
        public bool Inclusive { get; set; }
    }

    // DTO para los enlaces
    public class Link
    {
        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("rel")]
        public string Rel { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("encType")]
        public string EncType { get; set; }
    }
}
