using Newtonsoft.Json;

namespace GestorInventario.Application.DTOS.Paypal.Responses.POST.Subscription
{
    public record UpdatePricingPlanDto
    {
        [JsonProperty("pricing_schemes")]
        public required List<UpdatePricingSchemes> PricingSchemes { get; init; }
    }

    public record UpdatePricingSchemes
    {
        [JsonProperty("billing_cycle_sequence")]
        public int BillingCycleSequence { get; init; } 

        [JsonProperty("pricing_scheme")]
        public  UpdatePricingScheme? PricingScheme { get; init; }
    }

    public record UpdatePricingScheme
    {
        [JsonProperty("fixed_price")]
        public required UpdateFixedPrice FixedPrice { get; init; }

        [JsonProperty("pricing_model")]
        public  string? PricingModel { get; init; } 

        [JsonProperty("tiers")]
        public  List<Tier>? Tiers { get; init; } 
    }

    public record UpdateFixedPrice
    {
        [JsonProperty("value")]
        public string? Value { get; init; }

        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; init; }
    }

    public record Tier
    {
        [JsonProperty("starting_quantity")]
        public required string StartingQuantity { get; init; }

        [JsonProperty("ending_quantity")]
        public required string EndingQuantity { get; init; } 

        [JsonProperty("amount")]
        public required UpdateFixedPrice Amount { get; init; }
    }
}