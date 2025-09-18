namespace GestorInventario.Application.DTOs.Response_paypal.POST
{
    public class SuspendSubscriptionRequest
    {
        public required string Id { get; set; }
        public required string Reason { get; set; }
    }
}
