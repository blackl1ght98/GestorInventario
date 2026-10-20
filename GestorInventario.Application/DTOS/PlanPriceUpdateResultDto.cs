namespace GestorInventario.Application.DTOS;

public class PlanPriceUpdateResultDto
{
    public required string PlanId { get; init; }
    public List<PlanPriceSchemeDto> PricingSchemes { get; init; } = new();
}

public class PlanPriceSchemeDto
{
    public int BillingCycleSequence { get; init; }
    public required string Value { get; init; }
    public required string CurrencyCode { get; init; }
}