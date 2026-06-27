using Newtonsoft.Json;

namespace GestorInventario.Application.DTOS.Paypal.Responses.POST.Subscription
{
    //Dto para crear el plan de suscripcion
    public record PaypalPlanDetailsDto
    {
        [JsonProperty("id")]
        public  string? Id { get; init; } // ID del plan en PayPal

        [JsonProperty("product_id")]
        public required string ProductId { get; init; } // ID del producto asociado

        [JsonProperty("name")]
        public required string Name { get; init; } // Nombre del plan

        [JsonProperty("description")]
        public required string Description { get; init; } // Descripción del plan

        [JsonProperty("status")]
        public required string Status { get; init; } // Estado del plan (ej. ACTIVE)

        [JsonProperty("billing_cycles")]
        public required BillingCycleDto[] BillingCycles { get; init; } // Ciclos de facturación

        [JsonProperty("payment_preferences")]
        public PaymentPreferencesDto? PaymentPreferences { get; init; } // Preferencias de pago

        [JsonProperty("taxes")]
        public  TaxesDto? Taxes { get; init; } // Impuestos
    }

    public record BillingCycleDto
    {
        [JsonProperty("tenure_type")]
        public required string TenureType { get; init; } // TRIAL o REGULAR

        [JsonProperty("sequence")]
        public required int Sequence { get; init; } // Orden del ciclo (1, 2, etc.)

        [JsonProperty("frequency")]
        public required FrequencyDto Frequency { get; init; } // Frecuencia del ciclo

        [JsonProperty("total_cycles")]
        public required int TotalCycles { get; init; } // Número total de ciclos

        [JsonProperty("pricing_scheme")]
        public required PricingSchemeDto PricingScheme { get; init; } // Esquema de precios
    }

    public record FrequencyDto
    {
        [JsonProperty("interval_unit")]
        public required string IntervalUnit { get; init; } // DAY, WEEK, MONTH, YEAR

        [JsonProperty("interval_count")]
        public required int IntervalCount { get; init; } // Cantidad de intervalos
    }

    public record PricingSchemeDto
    {
        [JsonProperty("fixed_price")]
        public  FixedPriceDto FixedPrice { get; init; } // Precio fijo
    }

    public record FixedPriceDto
    {
        [JsonProperty("value")]
        public  string Value { get; init; } // Valor del precio

        [JsonProperty("currency_code")]
        public  string CurrencyCode { get; init; } // Código de moneda
    }

    public record PaymentPreferencesDto
    {
        [JsonProperty("auto_bill_outstanding")]
        public  bool AutoBillOutstanding { get; init; } // Preferencia de facturación automática

        [JsonProperty("setup_fee")]
        public  FixedPriceDto SetupFee { get; init; } // Tarifa inicial

        [JsonProperty("setup_fee_failure_action")]
        public  string SetupFeeFailureAction { get; init; } // Acción en caso de fallo de tarifa inicial

        [JsonProperty("payment_failure_threshold")]
        public  int PaymentFailureThreshold { get; init; } // Umbral de fallos de pago
    }

    public record TaxesDto
    {
        [JsonProperty("percentage")]
        public required string Percentage { get; init; } // Porcentaje de impuestos

        [JsonProperty("inclusive")]
        public required bool Inclusive { get; init; } // Si los impuestos son inclusivos
    }
}