using GestorInventario.Application.DTOs;

namespace GestorInventario.ViewModels.Paypal
{
    public class PlanesDto
    {
        public required string Id { get; set; }
        public required string productId { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Status { get; set; }
        public required string Usage_type { get; set; }
        public DateTime CreateTime { get; set; }
        public required List<BillingCycle> Billing_cycles { get; set; }
        public required Taxes Taxes { get; set; }
        public required string CurrencyCode { get; set; }
    }
}