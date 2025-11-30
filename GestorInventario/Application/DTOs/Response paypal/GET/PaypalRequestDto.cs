using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs
{
    public class PaypalRequestDto
    {
        public required string subscription_id { get; set; }
    }
    //DTO para obtener los detalles de una suscripcion
    public class PaypalSubscriptionResponse
    {
        [JsonProperty("id")]
        public  string? Id { get; set; }
        [JsonProperty("plan_id")]
        public required string PlanId { get; set; }
        [JsonProperty("status")]
        public required string Status { get; set; }
        [JsonProperty("start_time")]
        public DateTime? StartTime { get; set; }
        [JsonProperty("status_update_time")]
        public DateTime? StatusUpdateTime { get; set; }
        [JsonProperty("subscriber")]
        public required Subscriber Subscriber { get; set; }
        [JsonProperty("billing_info")]
        public required BillingInfo BillingInfo { get; set; }
    }

    public class Subscriber
    {
        [JsonProperty("name")]
        public required Name Name { get; set; }
        [JsonProperty("email_address")]
        public required string EmailAddress { get; set; }
        [JsonProperty("payer_id")]
        public required string PayerId { get; set; }
    }

    public class Name
    {
        [JsonProperty("given_name")]
        public required string GivenName { get; set; }
        [JsonProperty("surname")]
        public required string Surname { get; set; }
    }

    public class BillingInfo
    {
        [JsonProperty("outstanding_balance")]
        public required Amount OutstandingBalance { get; set; }
        [JsonProperty("next_billing_time")]
        public  DateTime? NextBillingTime { get; set; }
        [JsonProperty("last_payment")]
        public required LastPayment LastPayment { get; set; }
        [JsonProperty("final_payment_time")]
        public DateTime? FinalPaymentTime { get; set; }
        [JsonProperty("cycle_executions")]
        public  required List<CycleExecution> CycleExecutions { get; set; }
    }

    public class Amount
    {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; set; }
        [JsonProperty("value")]
        public required string Value { get; set; }
    }

    public class LastPayment
    {
        [JsonProperty("amount")]
        public required Amount Amount { get; set; }
        [JsonProperty("time")]
        public DateTime? Time { get; set; }
    }

    public class CycleExecution
    {
        [JsonProperty("cycles_completed")]
        public int CyclesCompleted { get; set; }
        [JsonProperty("cycles_remaining")]
        public int CyclesRemaining { get; set; }
        [JsonProperty("total_cycles")]
        public int TotalCycles { get; set; }
    }
}
