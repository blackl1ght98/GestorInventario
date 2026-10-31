namespace GestorInventario.Application.DTOS.Paypal;

public record PlanPriceUpdateResultDto
{
    public required string PlanId { get; init; }
    public List<PlanPriceSchemeDto> PricingSchemes { get; init; } = new();
}

public record PlanPriceSchemeDto
{
    public int BillingCycleSequence { get; init; }
    public required string Value { get; init; }
    public required string CurrencyCode { get; init; }
}