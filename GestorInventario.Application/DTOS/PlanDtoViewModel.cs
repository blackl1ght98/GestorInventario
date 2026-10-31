using GestorInventario.Application.DTOS.Paypal;

namespace GestorInventario.Application.DTOS;

public class PlanDtoViewModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public required List<CatalogPlanBillingCycleDto> Billing_cycles { get; init; }
    public required string Id { get; init; }
    public required string productId { get; init; }
    public string CurrencyCode { get; set; }
}