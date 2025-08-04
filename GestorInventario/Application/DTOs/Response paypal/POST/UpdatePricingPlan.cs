using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal.POST
{
    public class UpdatePricingPlan
    {
        [JsonProperty("pricing_schemes")]
        public List<UpdatePricingSchemes> PricingSchemes { get; set; }
    }

    public class UpdatePricingSchemes
    {
        [JsonProperty("billing_cycle_sequence")]
        public int BillingCycleSequence { get; set; } 

        [JsonProperty("pricing_scheme")]
        public UpdatePricingScheme PricingScheme { get; set; }
    }

    public class UpdatePricingScheme
    {
        [JsonProperty("fixed_price")]
        public UpdateFixedPrice FixedPrice { get; set; }

        [JsonProperty("pricing_model")]
        public string PricingModel { get; set; } 

        [JsonProperty("tiers")]
        public List<Tier> Tiers { get; set; } 
    }

    public class UpdateFixedPrice
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }
    }

    public class Tier
    {
        [JsonProperty("starting_quantity")]
        public string StartingQuantity { get; set; }

        [JsonProperty("ending_quantity")]
        public string EndingQuantity { get; set; } 

        [JsonProperty("amount")]
        public UpdateFixedPrice Amount { get; set; }
    }
}