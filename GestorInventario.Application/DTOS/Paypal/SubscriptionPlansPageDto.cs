namespace GestorInventario.Application.DTOS.Paypal;
public class SubscriptionPlansPageDto
{
    public List<SubscriptionPlan> Plans { get; init; } = new();
    public bool HasNextPage { get; init; }
}
public class SubscriptionPlan
{
    public required string Id { get; init; }
    public required string ProductId { get; init; }
    public required string Name { get; init; }
    public required string Status { get; init; }
    public required string Description { get; init; }
    public required List<CatalogPlanBillingCycleDto> BillingCycles { get; init; }
    public CatalogPlanPaymentPreferencesDto? PaymentPreferences { get; init; }
    public CatalogPlanTaxesDto? Taxes { get; init; }
    public bool QuantitySupported { get; init; }
    public DateTime CreateTime { get; init; }
    public DateTime UpdateTime { get; init; }
}
public class CatalogPlanBillingCycleDto
{
    public required string TenureType { get; init; }
    public int Sequence { get; init; }
    public int TotalCycles { get; init; }
    public required CatalogPlanFrequencyDto Frequency { get; init; }
    public required CatalogPlanPricingSchemeDto PricingScheme { get; init; }
}

public class CatalogPlanFrequencyDto
{
    public required string IntervalUnit { get; init; }
    public int IntervalCount { get; init; }
}

public class CatalogPlanPricingSchemeDto
{
    public int Version { get; init; }
    public required PlanMoneyDto FixedPrice { get; init; }
    public DateTime CreateTime { get; init; }
    public DateTime UpdateTime { get; init; }
}

public class PlanMoneyDto
{
    public required string CurrencyCode { get; init; }
    public required string Value { get; init; }
}

public class CatalogPlanPaymentPreferencesDto
{
    public string? ServiceType { get; init; }
    public bool AutoBillOutstanding { get; init; }
    public PlanMoneyDto? SetupFee { get; init; }
    public string? SetupFeeFailureAction { get; init; }
    public int PaymentFailureThreshold { get; init; }
}

public class CatalogPlanTaxesDto
{
    public required string Percentage { get; init; }
    public bool Inclusive { get; init; }
}


