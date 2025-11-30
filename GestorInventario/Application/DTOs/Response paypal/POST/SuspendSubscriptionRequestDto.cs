namespace GestorInventario.Application.DTOs.Response_paypal.POST
{
    public class SuspendSubscriptionRequestDto
    {
        public required string Id { get; set; }
        public required string Reason { get; set; }
    }
}
