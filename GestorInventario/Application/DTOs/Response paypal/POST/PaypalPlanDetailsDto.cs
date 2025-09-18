using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs
{
    //Dto para crear el plan de suscripcion
    public class PaypalPlanDetailsDto
    {
        [JsonProperty("id")]
        public  string? Id { get; set; } // ID del plan en PayPal

        [JsonProperty("product_id")]
        public required string ProductId { get; set; } // ID del producto asociado

        [JsonProperty("name")]
        public required string Name { get; set; } // Nombre del plan

        [JsonProperty("description")]
        public required string Description { get; set; } // Descripción del plan

        [JsonProperty("status")]
        public required string Status { get; set; } // Estado del plan (ej. ACTIVE)

        [JsonProperty("billing_cycles")]
        public required BillingCycleDto[] BillingCycles { get; set; } // Ciclos de facturación

        [JsonProperty("payment_preferences")]
        public required PaymentPreferencesDto PaymentPreferences { get; set; } // Preferencias de pago

        [JsonProperty("taxes")]
        public required TaxesDto Taxes { get; set; } // Impuestos
    }

    public class BillingCycleDto
    {
        [JsonProperty("tenure_type")]
        public required string TenureType { get; set; } // TRIAL o REGULAR

        [JsonProperty("sequence")]
        public required int Sequence { get; set; } // Orden del ciclo (1, 2, etc.)

        [JsonProperty("frequency")]
        public required FrequencyDto Frequency { get; set; } // Frecuencia del ciclo

        [JsonProperty("total_cycles")]
        public required int TotalCycles { get; set; } // Número total de ciclos

        [JsonProperty("pricing_scheme")]
        public required PricingSchemeDto PricingScheme { get; set; } // Esquema de precios
    }

    public class FrequencyDto
    {
        [JsonProperty("interval_unit")]
        public required string IntervalUnit { get; set; } // DAY, WEEK, MONTH, YEAR

        [JsonProperty("interval_count")]
        public required int IntervalCount { get; set; } // Cantidad de intervalos
    }

    public class PricingSchemeDto
    {
        [JsonProperty("fixed_price")]
        public required FixedPriceDto FixedPrice { get; set; } // Precio fijo
    }

    public class FixedPriceDto
    {
        [JsonProperty("value")]
        public required string Value { get; set; } // Valor del precio

        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; set; } // Código de moneda
    }

    public class PaymentPreferencesDto
    {
        [JsonProperty("auto_bill_outstanding")]
        public required bool AutoBillOutstanding { get; set; } // Preferencia de facturación automática

        [JsonProperty("setup_fee")]
        public required FixedPriceDto SetupFee { get; set; } // Tarifa inicial

        [JsonProperty("setup_fee_failure_action")]
        public required string SetupFeeFailureAction { get; set; } // Acción en caso de fallo de tarifa inicial

        [JsonProperty("payment_failure_threshold")]
        public required int PaymentFailureThreshold { get; set; } // Umbral de fallos de pago
    }

    public class TaxesDto
    {
        [JsonProperty("percentage")]
        public required string Percentage { get; set; } // Porcentaje de impuestos

        [JsonProperty("inclusive")]
        public required bool Inclusive { get; set; } // Si los impuestos son inclusivos
    }
}