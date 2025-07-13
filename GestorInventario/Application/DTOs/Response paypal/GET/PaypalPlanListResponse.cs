namespace GestorInventario.Application.DTOs.Response_paypal.GET
{
    public class PaypalPlanListResponseV2
    {
        public string id { get; set; }
        public string product_id { get; set; }
        public string name { get; set; }
        public string status { get; set; }
        public string description { get; set; }
        public string usage_type { get; set; }
        public List<BillingCyclev3> billing_cycles { get; set; }
        public PaymentPreferencesv3 payment_preferences { get; set; }
        public Taxesv3 taxes { get; set; }
        public bool? quantity_supported { get; set; }
        public DateTime? create_time { get; set; }
        public DateTime? update_time { get; set; }
        public List<Linkv3> links { get; set; }
    }

    public class BillingCyclev3
    {
        public PricingSchemev3 pricing_scheme { get; set; }
        public Frequencyv3 frequency { get; set; }
        public string tenure_type { get; set; }
        public int? sequence { get; set; }
        public int? total_cycles { get; set; }
    }

    public class PricingSchemev3
    {
        public int? version { get; set; }
        public Moneyv3 fixed_price { get; set; }
        public DateTime? create_time { get; set; }
        public DateTime? update_time { get; set; }
    }

    public class Moneyv3
    {
        public string currency_code { get; set; }
        public string value { get; set; }
    }

    public class Frequencyv3
    {
        public string interval_unit { get; set; }
        public int? interval_count { get; set; }
    }

    public class PaymentPreferencesv3
    {
        public string service_type { get; set; }
        public bool? auto_bill_outstanding { get; set; }
        public Moneyv3 setup_fee { get; set; }
        public string setup_fee_failure_action { get; set; }
        public int? payment_failure_threshold { get; set; }
    }

    public class Taxesv3
    {
        public string percentage { get; set; }
        public bool? inclusive { get; set; }
    }

    public class Linkv3
    {
        public string href { get; set; }
        public string rel { get; set; }
        public string method { get; set; }
        public string encType { get; set; }
    }
}
