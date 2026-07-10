using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;


namespace GestorInventario.Shared.DTOS.Paypal.Responses.GET.Subscription
{
    public record PaypalPlansListResponse
    {
        [JsonProperty("plans")]
        public required List<PaypalPlanResponseDto> Plans { get; init; }

        [JsonProperty("links")]
        public required List<Link> Links { get; init; }
    }
    public record PaypalPlanResponseDto
    {
        [JsonProperty("id")]
        [Required(ErrorMessage = "El ID del plan es requerido.")]
        public required string Id { get; init; }

        [JsonProperty("product_id")]
        [Required(ErrorMessage = "El ID del producto es requerido.")]
        public required string ProductId { get; init; }

        [JsonProperty("name")]
        [Required(ErrorMessage = "El nombre del plan es requerido.")]
        public required string Name { get; init; }

        [JsonProperty("status")]
        [Required(ErrorMessage = "El estado del plan es requerido.")]
        public required string Status { get; init; }

        [JsonProperty("description")]
        public required string Description { get; init; }

        [JsonProperty("usage_type")]
        public required string UsageType { get; init; }

        [JsonProperty("billing_cycles")]
        [Required(ErrorMessage = "Los ciclos de facturación son requeridos.")]
        public required List<BillingCycle> BillingCycles { get; init; }

        [JsonProperty("payment_preferences")]
        [Required(ErrorMessage = "Las preferencias de pago son requeridas.")]
        public required PaymentPreferences PaymentPreferences { get; init; }

        [JsonProperty("taxes")]
        public required Taxes Taxes { get; init; }

        [JsonProperty("quantity_supported")]
        public required bool QuantitySupported { get; init; }

        [JsonProperty("create_time")]
        public required DateTime CreateTime { get; init; }

        [JsonProperty("update_time")]
        public required DateTime UpdateTime { get; init; }

        [JsonProperty("links")]
        public required List<Link> Links { get; init; }
    }

    // DTO para los ciclos de facturación
    public record BillingCycle
    {
        [JsonProperty("pricing_scheme")]
        [Required(ErrorMessage = "El esquema de precios es requerido.")]
        public required PricingScheme PricingScheme { get; init; }

        [JsonProperty("frequency")]
        [Required(ErrorMessage = "La frecuencia es requerida.")]
        public required Frequency Frequency { get; init; }

        [JsonProperty("tenure_type")]
        [Required(ErrorMessage = "El tipo de tenencia es requerido.")]
        public required string TenureType { get; init; }

        [JsonProperty("sequence")]
        public   int Sequence { get; init; }

        [JsonProperty("total_cycles")]
        public int TotalCycles { get; init; }
    }

    // DTO para el esquema de precios
    public record PricingScheme
    {
        [JsonProperty("version")]
        public int Version { get; init; }

        [JsonProperty("fixed_price")]
        [Required(ErrorMessage = "El precio fijo es requerido.")]
        public required Money FixedPrice { get; init; }

        [JsonProperty("create_time")]
        public DateTime CreateTime { get; init; }

        [JsonProperty("update_time")]
        public DateTime UpdateTime { get; init; }
    }

    // DTO para el monto
    public record Money
    {
        [JsonProperty("currency_code")]
        [Required(ErrorMessage = "El código de moneda es requerido.")]
        public required string CurrencyCode { get; init; }

        [JsonProperty("value")]
        [Required(ErrorMessage = "El valor del monto es requerido.")]
        public required string Value { get; init; }
    }

    // DTO para la frecuencia
    public record Frequency
    {
        [JsonProperty("interval_unit")]
        [Required(ErrorMessage = "La unidad de intervalo es requerida.")]
        public required string IntervalUnit { get; init; }

        [JsonProperty("interval_count")]
        public int IntervalCount { get; init; }
    }

    // DTO para las preferencias de pago
    public record PaymentPreferences
    {
        [JsonProperty("service_type")]
        public required string ServiceType { get; init; }

        [JsonProperty("auto_bill_outstanding")]
        public bool AutoBillOutstanding { get; init; }

        [JsonProperty("setup_fee")]
        public required Money SetupFee { get; init; }

        [JsonProperty("setup_fee_failure_action")]
        public required string SetupFeeFailureAction { get; init; }

        [JsonProperty("payment_failure_threshold")]
        public int PaymentFailureThreshold { get; init; }
    }

    // DTO para los impuestos
    public record Taxes
    {
        [JsonProperty("percentage")]
        public required string Percentage { get; init; }

        [JsonProperty("inclusive")]
        public bool Inclusive { get; init; }
    }

    // DTO para los enlaces
    public record Link
    {
        [JsonProperty("href")]
        public required string Href { get; init; }

        [JsonProperty("rel")]
        public required string Rel { get; init; }

        [JsonProperty("method")]
        public required string Method { get; init; }

        [JsonProperty("encType")]
        public required string EncType { get; init; }
    }
}
