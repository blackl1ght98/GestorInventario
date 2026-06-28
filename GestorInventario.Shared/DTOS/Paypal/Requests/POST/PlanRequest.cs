
using Newtonsoft.Json;

namespace GestorInventario.Shared.DTOS.Paypal.Requests.POST
{
    public record PlanRequest
     {

        [JsonProperty("product_id")]
        public required string ProductId { get; init; } // ID del producto asociado

        [JsonProperty("name")]
        public required string Name { get; init; } // Nombre del plan

        [JsonProperty("description")]
        public required string Description { get; init; } // Descripción del plan

        [JsonProperty("status")]
        public required string Status { get; init; } // Estado del plan (ej. ACTIVE)

        [JsonProperty("billing_cycles")]
        public required BillingCycleRq[] BillingCycles { get; init; } // Ciclos de facturación

        [JsonProperty("payment_preferences")]
        public PaymentPreferencesRq? PaymentPreferences { get; init; } // Preferencias de pago

        [JsonProperty("taxes")]
        public TaxesRq? Taxes { get; init; } // Impuestos
    }
    public record BillingCycleRq
    {
        [JsonProperty("tenure_type")]
        public required string TenureType { get; init; } // TRIAL o REGULAR

        [JsonProperty("sequence")]
        public required int Sequence { get; init; } // Orden del ciclo (1, 2, etc.)

        [JsonProperty("frequency")]
        public required FrequencyRq Frequency { get; init; } // Frecuencia del ciclo

        [JsonProperty("total_cycles")]
        public required int TotalCycles { get; init; } // Número total de ciclos

        [JsonProperty("pricing_scheme")]
        public required PricingSchemeRq PricingScheme { get; init; } // Esquema de precios
    }

    public record FrequencyRq
    {
        [JsonProperty("interval_unit")]
        public required string IntervalUnit { get; init; } // DAY, WEEK, MONTH, YEAR

        [JsonProperty("interval_count")]
        public required int IntervalCount { get; init; } // Cantidad de intervalos
    }

    public record PricingSchemeRq
    {
        [JsonProperty("fixed_price")]
        public FixedPriceRq FixedPrice { get; init; } // Precio fijo
    }

    public record FixedPriceRq
    {
        [JsonProperty("value")]
        public  string Value { get; init; } // Valor del precio

        [JsonProperty("currency_code")]
        public  string CurrencyCode { get; init; } // Código de moneda
    }

    public record PaymentPreferencesRq
    {
        [JsonProperty("auto_bill_outstanding")]
        public  bool AutoBillOutstanding { get; init; } // Preferencia de facturación automática

        [JsonProperty("setup_fee")]
        public FixedPriceRq SetupFee { get; init; } // Tarifa inicial

        [JsonProperty("setup_fee_failure_action")]
        public  string SetupFeeFailureAction { get; init; } // Acción en caso de fallo de tarifa inicial

        [JsonProperty("payment_failure_threshold")]
        public  int PaymentFailureThreshold { get; init; } // Umbral de fallos de pago
    }

    public record TaxesRq
    {
        [JsonProperty("percentage")]
        public required string Percentage { get; init; } // Porcentaje de impuestos

        [JsonProperty("inclusive")]
        public required bool Inclusive { get; init; } // Si los impuestos son inclusivos
    }
}
