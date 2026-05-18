using Newtonsoft.Json;

namespace GestorInventario.Application.DTOS.Paypal.Requests.POST
{
    public class PaypalPlanRequest
    {
        [JsonProperty("id")]
        public string? Id { get; set; } // ID del plan en PayPal

        [JsonProperty("product_id")]
        public required string ProductId { get; set; } // ID del producto asociado

        [JsonProperty("name")]
        public required string Name { get; set; } // Nombre del plan

        [JsonProperty("description")]
        public required string Description { get; set; } // Descripción del plan

        [JsonProperty("status")]
        public required string Status { get; set; } // Estado del plan (ej. ACTIVE)

        [JsonProperty("billing_cycles")]
        public required BillingCycleRequest[] BillingCycles { get; set; } // Ciclos de facturación

        [JsonProperty("payment_preferences")]
        public required PaymentPreferencesRequest PaymentPreferences { get; set; } // Preferencias de pago

        [JsonProperty("taxes")]
        public required TaxesRequest Taxes { get; set; } // Impuestos
    }

    public class BillingCycleRequest
    {
        [JsonProperty("tenure_type")]
        public required string TenureType { get; set; } // TRIAL o REGULAR

        [JsonProperty("sequence")]
        public required int Sequence { get; set; } // Orden del ciclo (1, 2, etc.)

        [JsonProperty("frequency")]
        public required FrequencyRequest Frequency { get; set; } // Frecuencia del ciclo

        [JsonProperty("total_cycles")]
        public required int TotalCycles { get; set; } // Número total de ciclos

        [JsonProperty("pricing_scheme")]
        public required PricingSchemeRequest PricingScheme { get; set; } // Esquema de precios
    }

    public class FrequencyRequest
    {
        [JsonProperty("interval_unit")]
        public required string IntervalUnit { get; set; } // DAY, WEEK, MONTH, YEAR

        [JsonProperty("interval_count")]
        public required int IntervalCount { get; set; } // Cantidad de intervalos
    }

    public class PricingSchemeRequest
    {
        [JsonProperty("fixed_price")]
        public required FixedPriceRequest FixedPrice { get; set; } // Precio fijo
    }

    public class FixedPriceRequest
    {
        [JsonProperty("value")]
        public required string Value { get; set; } // Valor del precio

        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; set; } // Código de moneda
    }

    public class PaymentPreferencesRequest
    {
        [JsonProperty("auto_bill_outstanding")]
        public required bool AutoBillOutstanding { get; set; } // Preferencia de facturación automática

        [JsonProperty("setup_fee")]
        public required FixedPriceRequest SetupFee { get; set; } // Tarifa inicial

        [JsonProperty("setup_fee_failure_action")]
        public required string SetupFeeFailureAction { get; set; } // Acción en caso de fallo de tarifa inicial

        [JsonProperty("payment_failure_threshold")]
        public required int PaymentFailureThreshold { get; set; } // Umbral de fallos de pago
    }

    public class TaxesRequest
    {
        [JsonProperty("percentage")]
        public required string Percentage { get; set; } // Porcentaje de impuestos

        [JsonProperty("inclusive")]
        public required bool Inclusive { get; set; } // Si los impuestos son inclusivos
    }
}
