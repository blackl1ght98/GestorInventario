using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs
{
    //Dto para crear el plan de suscripcion
    public class PaypalPlanDetailsDto
    {
        [JsonProperty("id")]
        public string Id { get; set; } // ID del plan en PayPal

        [JsonProperty("product_id")]
        public string ProductId { get; set; } // ID del producto asociado

        [JsonProperty("name")]
        public string Name { get; set; } // Nombre del plan

        [JsonProperty("description")]
        public string Description { get; set; } // Descripción del plan

        [JsonProperty("status")]
        public string Status { get; set; } // Estado del plan (ej. ACTIVE)

        [JsonProperty("billing_cycles")]
        public BillingCycleDto[] BillingCycles { get; set; } // Ciclos de facturación

        [JsonProperty("payment_preferences")]
        public PaymentPreferencesDto PaymentPreferences { get; set; } // Preferencias de pago

        [JsonProperty("taxes")]
        public TaxesDto Taxes { get; set; } // Impuestos
    }

    public class BillingCycleDto
    {
        [JsonProperty("tenure_type")]
        public string TenureType { get; set; } // TRIAL o REGULAR

        [JsonProperty("sequence")]
        public int Sequence { get; set; } // Orden del ciclo (1, 2, etc.)

        [JsonProperty("frequency")]
        public FrequencyDto Frequency { get; set; } // Frecuencia del ciclo

        [JsonProperty("total_cycles")]
        public int TotalCycles { get; set; } // Número total de ciclos

        [JsonProperty("pricing_scheme")]
        public PricingSchemeDto PricingScheme { get; set; } // Esquema de precios
    }

    public class FrequencyDto
    {
        [JsonProperty("interval_unit")]
        public string IntervalUnit { get; set; } // DAY, WEEK, MONTH, YEAR

        [JsonProperty("interval_count")]
        public int IntervalCount { get; set; } // Cantidad de intervalos
    }

    public class PricingSchemeDto
    {
        [JsonProperty("fixed_price")]
        public FixedPriceDto FixedPrice { get; set; } // Precio fijo
    }

    public class FixedPriceDto
    {
        [JsonProperty("value")]
        public string Value { get; set; } // Valor del precio

        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; } // Código de moneda
    }

    public class PaymentPreferencesDto
    {
        [JsonProperty("auto_bill_outstanding")]
        public bool AutoBillOutstanding { get; set; } // Preferencia de facturación automática

        [JsonProperty("setup_fee")]
        public FixedPriceDto SetupFee { get; set; } // Tarifa inicial

        [JsonProperty("setup_fee_failure_action")]
        public string SetupFeeFailureAction { get; set; } // Acción en caso de fallo de tarifa inicial

        [JsonProperty("payment_failure_threshold")]
        public int PaymentFailureThreshold { get; set; } // Umbral de fallos de pago
    }

    public class TaxesDto
    {
        [JsonProperty("percentage")]
        public string Percentage { get; set; } // Porcentaje de impuestos

        [JsonProperty("inclusive")]
        public bool Inclusive { get; set; } // Si los impuestos son inclusivos
    }
}