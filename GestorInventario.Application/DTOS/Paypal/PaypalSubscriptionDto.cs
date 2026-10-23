namespace GestorInventario.Application.DTOS.Paypal;

public class PaypalSubscriptionDto
{
  
        public  string? Id { get; init; }//
      
        public required string PlanId { get; init; }
       
        public required string Status { get; init; }//
       
        public DateTime? StartTime { get; init; }
        
        public DateTime? StatusUpdateTime { get; init; }
       
        public required SubscriberDto Subscriber { get; init; }
       
        public required BillingInfoDto BillingInfo { get; init; }
    }

    public record SubscriberDto
    {
       
        public required NameDto Name { get; init; }
       
        public required string EmailAddress { get; init; }
     
        public required string PayerId { get; init; }
    }

    public record NameDto
    {
        
        public required string GivenName { get; init; }
     
        public required string Surname { get; init; }
    }

    public record BillingInfoDto
    {
        
        public required AmountDto OutstandingBalance { get; init; }
       
        public  DateTime? NextBillingTime { get; init; }
        
        public required LastPaymentDto LastPayment { get; init; }
      
        public DateTime? FinalPaymentTime { get; init; }
       
        public  required List<CycleExecutionDto> CycleExecutions { get; init; }
    }

    public record AmountDto
    {
     
        public required string CurrencyCode { get; init; }
       
        public required string Value { get; init; }
    }

    public record LastPaymentDto
    {
        
        public required AmountDto Amount { get; init; }
        
        public DateTime? Time { get; init; }
    }

    public record CycleExecutionDto
    {
        
        public int CyclesCompleted { get; init; }
       
        public int CyclesRemaining { get; init; }
       
        public int TotalCycles { get; init; }
    }