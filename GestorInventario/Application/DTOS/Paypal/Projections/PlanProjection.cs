using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOS.Paypal.Responses.GET.Subscription;

namespace GestorInventario.Application.DTOS.Paypal.Projections
{
    public record PlanProjection
    {
        public required string Id { get; init; }
        public required string productId { get; init; }
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required string Status { get; init; }
        public required string Usage_type { get; init; }
        public DateTime CreateTime { get; init; }
        public required List<BillingCycle> Billing_cycles { get; init; }
        public required Taxes Taxes { get; init; }
        public required string CurrencyCode { get; init; }
    }
}