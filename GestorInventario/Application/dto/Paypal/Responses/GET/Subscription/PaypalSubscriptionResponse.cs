using Newtonsoft.Json;

namespace GestorInventario.Application.DTOS.Paypal.Responses.GET.Subscription
{
   
    //DTO para obtener los detalles de una suscripcion
    public record PaypalSubscriptionResponse
    {
        [JsonProperty("id")]
        public  string? Id { get; init; }
        [JsonProperty("plan_id")]
        public required string PlanId { get; init; }
        [JsonProperty("status")]
        public required string Status { get; init; }
        [JsonProperty("start_time")]
        public DateTime? StartTime { get; init; }
        [JsonProperty("status_update_time")]
        public DateTime? StatusUpdateTime { get; init; }
        [JsonProperty("subscriber")]
        public required Subscriber Subscriber { get; init; }
        [JsonProperty("billing_info")]
        public required BillingInfo BillingInfo { get; init; }
    }

    public record Subscriber
    {
        [JsonProperty("name")]
        public required Name Name { get; init; }
        [JsonProperty("email_address")]
        public required string EmailAddress { get; init; }
        [JsonProperty("payer_id")]
        public required string PayerId { get; init; }
    }

    public record Name
    {
        [JsonProperty("given_name")]
        public required string GivenName { get; init; }
        [JsonProperty("surname")]
        public required string Surname { get; init; }
    }

    public record BillingInfo
    {
        [JsonProperty("outstanding_balance")]
        public required Amount OutstandingBalance { get; init; }
        [JsonProperty("next_billing_time")]
        public  DateTime? NextBillingTime { get; init; }
        [JsonProperty("last_payment")]
        public required LastPayment LastPayment { get; init; }
        [JsonProperty("final_payment_time")]
        public DateTime? FinalPaymentTime { get; init; }
        [JsonProperty("cycle_executions")]
        public  required List<CycleExecution> CycleExecutions { get; init; }
    }

    public record Amount
    {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; init; }
        [JsonProperty("value")]
        public required string Value { get; init; }
    }

    public record LastPayment
    {
        [JsonProperty("amount")]
        public required Amount Amount { get; init; }
        [JsonProperty("time")]
        public DateTime? Time { get; init; }
    }

    public record CycleExecution
    {
        [JsonProperty("cycles_completed")]
        public int CyclesCompleted { get; init; }
        [JsonProperty("cycles_remaining")]
        public int CyclesRemaining { get; init; }
        [JsonProperty("total_cycles")]
        public int TotalCycles { get; init; }
    }
}
