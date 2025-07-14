namespace GestorInventario.Application.DTOs
{
    public class PaypalRequest
    {
        public string subscription_id { get; set; }
    }
    //DTO para obtener los detalles de una suscripcion
    public class PaypalSubscriptionResponse
    {
        public string id { get; set; }
        public string plan_id { get; set; }
        public string status { get; set; }
        public DateTime? start_time { get; set; }
        public DateTime? status_update_time { get; set; }
        public Subscriber subscriber { get; set; }
        public BillingInfo billing_info { get; set; }
    }

    public class Subscriber
    {
        public Name name { get; set; }
        public string email_address { get; set; }
        public string payer_id { get; set; }
    }

    public class Name
    {
        public string given_name { get; set; }
        public string surname { get; set; }
    }

    public class BillingInfo
    {
        public Amount outstanding_balance { get; set; }
        public DateTime? next_billing_time { get; set; }
        public LastPayment last_payment { get; set; }
        public DateTime? final_payment_time { get; set; }
        public List<CycleExecution> cycle_executions { get; set; }
    }

    public class Amount
    {
        public string currency_code { get; set; }
        public string value { get; set; }
    }

    public class LastPayment
    {
        public Amount amount { get; set; }
        public DateTime? time { get; set; }
    }

    public class CycleExecution
    {
        public int cycles_completed { get; set; }
        public int cycles_remaining { get; set; }
        public int total_cycles { get; set; }
    }
}
