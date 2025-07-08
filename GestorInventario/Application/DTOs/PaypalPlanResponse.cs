using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GestorInventario.Application.DTOs
{
    public class PaypalPlanResponse
    {
        [JsonPropertyName("id")]
        [Required(ErrorMessage = "El ID del plan es requerido.")]
        public string Id { get; set; }

        [JsonPropertyName("product_id")]
        [Required(ErrorMessage = "El ID del producto es requerido.")]
        public string ProductId { get; set; }

        [JsonPropertyName("name")]
        [Required(ErrorMessage = "El nombre del plan es requerido.")]
        public string Name { get; set; }

        [JsonPropertyName("status")]
        [Required(ErrorMessage = "El estado del plan es requerido.")]
        public string Status { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("usage_type")]
        public string UsageType { get; set; }

        [JsonPropertyName("billing_cycles")]
        [Required(ErrorMessage = "Los ciclos de facturación son requeridos.")]
        public List<BillingCycle> BillingCycles { get; set; }

        [JsonPropertyName("payment_preferences")]
        [Required(ErrorMessage = "Las preferencias de pago son requeridas.")]
        public PaymentPreferences PaymentPreferences { get; set; }

        [JsonPropertyName("taxes")]
        public Taxes Taxes { get; set; }

        [JsonPropertyName("quantity_supported")]
        public bool QuantitySupported { get; set; }

        [JsonPropertyName("create_time")]
        public DateTime CreateTime { get; set; }

        [JsonPropertyName("update_time")]
        public DateTime UpdateTime { get; set; }

        [JsonPropertyName("links")]
        public List<Link> Links { get; set; }
    }

    // DTO para los ciclos de facturación
    public class BillingCycle
    {
        [JsonPropertyName("pricing_scheme")]
        [Required(ErrorMessage = "El esquema de precios es requerido.")]
        public PricingScheme PricingScheme { get; set; }

        [JsonPropertyName("frequency")]
        [Required(ErrorMessage = "La frecuencia es requerida.")]
        public Frequency Frequency { get; set; }

        [JsonPropertyName("tenure_type")]
        [Required(ErrorMessage = "El tipo de tenencia es requerido.")]
        public string TenureType { get; set; }

        [JsonPropertyName("sequence")]
        public int Sequence { get; set; }

        [JsonPropertyName("total_cycles")]
        public int TotalCycles { get; set; }
    }

    // DTO para el esquema de precios
    public class PricingScheme
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("fixed_price")]
        [Required(ErrorMessage = "El precio fijo es requerido.")]
        public Money FixedPrice { get; set; }

        [JsonPropertyName("create_time")]
        public DateTime CreateTime { get; set; }

        [JsonPropertyName("update_time")]
        public DateTime UpdateTime { get; set; }
    }

    // DTO para el monto
    public class Money
    {
        [JsonPropertyName("currency_code")]
        [Required(ErrorMessage = "El código de moneda es requerido.")]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("value")]
        [Required(ErrorMessage = "El valor del monto es requerido.")]
        public string Value { get; set; }
    }

    // DTO para la frecuencia
    public class Frequency
    {
        [JsonPropertyName("interval_unit")]
        [Required(ErrorMessage = "La unidad de intervalo es requerida.")]
        public string IntervalUnit { get; set; }

        [JsonPropertyName("interval_count")]
        public int IntervalCount { get; set; }
    }

    // DTO para las preferencias de pago
    public class PaymentPreferences
    {
        [JsonPropertyName("service_type")]
        public string ServiceType { get; set; }

        [JsonPropertyName("auto_bill_outstanding")]
        public bool AutoBillOutstanding { get; set; }

        [JsonPropertyName("setup_fee")]
        public Money SetupFee { get; set; }

        [JsonPropertyName("setup_fee_failure_action")]
        public string SetupFeeFailureAction { get; set; }

        [JsonPropertyName("payment_failure_threshold")]
        public int PaymentFailureThreshold { get; set; }
    }

    // DTO para los impuestos
    public class Taxes
    {
        [JsonPropertyName("percentage")]
        public string Percentage { get; set; }

        [JsonPropertyName("inclusive")]
        public bool Inclusive { get; set; }
    }

    // DTO para los enlaces
    public class Link
    {
        [JsonPropertyName("href")]
        public string Href { get; set; }

        [JsonPropertyName("rel")]
        public string Rel { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("encType")]
        public string EncType { get; set; }
    }
}
