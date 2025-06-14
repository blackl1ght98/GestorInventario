namespace GestorInventario.Domain.Models.ViewModels.Paypal
{
    namespace GestorInventario.Domain.Models.ViewModels.Paypal
    {
        public class PlanesViewModel
        {
            public string id { get; set; }
            public string productId { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public string status { get; set; }
            public string usage_type { get; set; }
            public string createTime { get; set; }
            public List<BillingCycle> billing_cycles { get; set; }
            public Taxes Taxes { get; set; }
        }

        // Las siguientes clases son necesarias para deserializar BillingCycles y Taxes
        public class BillingCycle
        {
            public FrequencyPlans frequency { get; set; }
            public string tenure_type { get; set; }
            public int sequence { get; set; }
            public int total_cycles { get; set; }
            public PricingScheme pricing_scheme { get; set; }
        }

        public class FrequencyPlans
        {
            public string interval_unit { get; set; }
            public int interval_count { get; set; }
        }

        public class PricingScheme
        {
            public Money fixed_price { get; set; }
            public string status { get; set; }
            public int version { get; set; }
            public string create_time { get; set; }
            public string update_time { get; set; }
        }

        public class Money
        {
            public string value { get; set; }
            public string currency_code { get; set; }
        }

        public class Taxes
        {
            public string percentage { get; set; }
            public bool inclusive { get; set; }
        }
    }
}
