namespace GestorInventario.Application.DTOS.Paypal;

public record PlanDetailsDto
{
    public string? Id { get; init; }
    public required string ProductId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Status { get; init; }
    public List<PlanBillingCycleDto> BillingCycles { get; init; } = new();
    public PlanPaymentPreferencesDto? PaymentPreferences { get; init; }
    public PlanTaxesDto? Taxes { get; init; }
}

public record PlanBillingCycleDto
{
    public required string TenureType { get; init; }   // TRIAL | REGULAR
    public int Sequence { get; init; }
    public required PlanFrequencyDto Frequency { get; init; }
    public int TotalCycles { get; init; }
    public required PlanPricingSchemeDto PricingScheme { get; init; }
}

public record PlanFrequencyDto
{
    public required string IntervalUnit { get; init; } // DAY, MONTH, YEAR...
    public int IntervalCount { get; init; }
}

public record PlanPricingSchemeDto
{
    public PlanFixedPriceDto? FixedPrice { get; init; }
}

public record PlanFixedPriceDto
{
    public string? Value { get; init; }
    public string? CurrencyCode { get; init; }
}

public record PlanPaymentPreferencesDto
{
    public bool AutoBillOutstanding { get; init; }
    public PlanFixedPriceDto? SetupFee { get; init; }
    public string? SetupFeeFailureAction { get; init; }
    public int PaymentFailureThreshold { get; init; }
}

public record PlanTaxesDto
{
    public required string Percentage { get; init; }
    public bool Inclusive { get; init; }
}