namespace GestorInventario.Application.DTOS.Paypal;

public class SubcritionDtoViewModel
{
    public string SubscriptionId { get; set; }
    public string Status { get; set; }
    public string PlanId { get; set; }
    public string SubscriberName { get; set; } 

    public string SubscriberEmail { get; set; }
    public string PayerId { get; set; }
    public int? TrialIntervalCount { get; set; }
    public string? NombreCompleto { get; set; }
    
}