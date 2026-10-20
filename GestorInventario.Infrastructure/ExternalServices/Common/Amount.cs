using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.ExternalServices.Common;

public record Amount
{
    [JsonProperty("currency_code")]
    public required string CurrencyCode { get; init; }
    [JsonProperty("value")]
    public required string Value { get; init; }
    [JsonProperty("breakdown")]
    public required AmountBreakdown Breakdown { get; init; }
}
public record AmountBreakdown
{
    [JsonProperty("item_total")]
    public required Money ItemTotal { get; init; }

    [JsonProperty("tax_total")]
    public required Money TaxTotal { get; init; }

    [JsonProperty("shipping")]
    public required Money ShippingAmount { get; init; }
}