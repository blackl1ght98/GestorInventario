

namespace GestorInventario.Application.DTOS.Configuration
{
    public class PaypalSettings
    {
        public const string SectionName = "Paypal";
        public required ReturnUrlSet ReturnUrls { get; set; }
        public required CancelUrlSet CancelUrls { get; set; } 
    }
    public record ReturnUrlSet
    {
        public required string Development { get; set; } 
        public required string Docker { get; set; } 
    }
    public record CancelUrlSet
    {
        public required string Development { get; set; } 
        public required string Docker { get; set; }
    }
}
