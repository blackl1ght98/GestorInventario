using GestorInventario.Application.DTOS.Paypal.Responses.GET.Subscription;
using Newtonsoft.Json;

namespace GestorInventario.Application.DTOS.Paypal.Requests.POST
{
    public class PlanRequest
     {

        [JsonProperty("product_id")]
        public required string ProductId { get; set; } // ID del producto asociado

        [JsonProperty("name")]
        public required string Name { get; set; } // Nombre del plan

        [JsonProperty("description")]
        public required string Description { get; set; } // Descripción del plan

        [JsonProperty("status")]
        public required string Status { get; set; } // Estado del plan (ej. ACTIVE)

        [JsonProperty("billing_cycles")]
        public required BillingCycleRq[] BillingCycles { get; set; } // Ciclos de facturación

        [JsonProperty("payment_preferences")]
        public PaymentPreferencesRq? PaymentPreferences { get; set; } // Preferencias de pago

        [JsonProperty("taxes")]
        public TaxesRq? Taxes { get; set; } // Impuestos
    }

    public class BillingCycleRq
    {
        [JsonProperty("tenure_type")]
        public required string TenureType { get; set; } // TRIAL o REGULAR

        [JsonProperty("sequence")]
        public required int Sequence { get; set; } // Orden del ciclo (1, 2, etc.)

        [JsonProperty("frequency")]
        public required FrequencyRq Frequency { get; set; } // Frecuencia del ciclo

        [JsonProperty("total_cycles")]
        public required int TotalCycles { get; set; } // Número total de ciclos

        [JsonProperty("pricing_scheme")]
        public required PricingSchemeRq PricingScheme { get; set; } // Esquema de precios
    }

    public class FrequencyRq
    {
        [JsonProperty("interval_unit")]
        public required string IntervalUnit { get; set; } // DAY, WEEK, MONTH, YEAR

        [JsonProperty("interval_count")]
        public required int IntervalCount { get; set; } // Cantidad de intervalos
    }

    public class PricingSchemeRq
    {
        [JsonProperty("fixed_price")]
        public FixedPriceRq FixedPrice { get; set; } // Precio fijo
    }

    public class FixedPriceRq
    {
        [JsonProperty("value")]
        public  string Value { get; set; } // Valor del precio

        [JsonProperty("currency_code")]
        public  string CurrencyCode { get; set; } // Código de moneda
    }

    public class PaymentPreferencesRq
    {
        [JsonProperty("auto_bill_outstanding")]
        public  bool AutoBillOutstanding { get; set; } // Preferencia de facturación automática

        [JsonProperty("setup_fee")]
        public FixedPriceRq SetupFee { get; set; } // Tarifa inicial

        [JsonProperty("setup_fee_failure_action")]
        public  string SetupFeeFailureAction { get; set; } // Acción en caso de fallo de tarifa inicial

        [JsonProperty("payment_failure_threshold")]
        public  int PaymentFailureThreshold { get; set; } // Umbral de fallos de pago
    }

    public class TaxesRq
    {
        [JsonProperty("percentage")]
        public required string Percentage { get; set; } // Porcentaje de impuestos

        [JsonProperty("inclusive")]
        public required bool Inclusive { get; set; } // Si los impuestos son inclusivos
    }
}
