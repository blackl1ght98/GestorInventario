using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal.POST
{
    public class PaypalCaptureOrder
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("payment_source")]
        public PaymentSourceCapture PaymentSource { get; set; }
        [JsonProperty("purchase_units")]
        public List<PurchaseUnitsCapture> PurchaseUnits { get; set; }
        [JsonProperty("payer")]
        public PayerCapture PayerCapture { get; set; }
        [JsonProperty("links")]
        public List<LinksCapture> Links { get; set; }
    }
    public class PaymentSourceCapture
    {
        [JsonProperty("paypal")]
        public PaypalCapture Paypal { get; set; }
    }
    public class PaypalCapture
    {
        [JsonProperty("name")]
        public NameCapture Name { get; set; }
        [JsonProperty("email_address")]
        public string EmailAddress { get; set; }
        [JsonProperty("account_id")]
        public string AccountId { get; set; }
    }
    public class NameCapture {
        [JsonProperty("given_name")]
        public string GivenName { get; set; }
        [JsonProperty("surname")]
        public string Surname { get; set; }

    }
    public class PurchaseUnitsCapture
    {
        [JsonProperty("reference_id")]
        public string ReferenceId { get; set; }
        [JsonProperty("shipping")]
        public ShippingCapture Shipping { get; set; }
        [JsonProperty("payments")]
        public PaymentsCapture Payments { get; set; }
    }
    public class ShippingCapture {
        [JsonProperty("address")]
        public AddressCapture Address { get; set; }
    
    }
    public class AddressCapture {
        [JsonProperty("address_line_1")]
        public string AddressLine1 { get; set; }
        [JsonProperty("address_line_2")]
        public string AddressLine2 { get; set; }
        [JsonProperty("admin_area_2")]
        public string AdminArea2 { get; set; }
        [JsonProperty("admin_area_1")]
        public string AdminArea1 { get; set; }
        [JsonProperty("postal_code")]
        public string PostalCode { get; set; }
        [JsonProperty("country_code")]
        public string CountryCode { get; set; }
    }
    public class PaymentsCapture
    {
        [JsonProperty("captures")]
        public List<Captures> Captures { get; set; }
    }
    public class Captures
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("amount")]
        public AmountCapture Amount { get; set; }
        [JsonProperty("seller_protection")]
        public SellerProtectionCapture SellerProtection { get; set; }
        [JsonProperty("final_capture")]
        public bool FinalCapture { get; set; }
        [JsonProperty("disbursement_mode")]
        public string DisbursementMode { get; set; }
        [JsonProperty("seller_receivable_breakdown")]
        public SellerReceivableBreakdownCapture SellerReceivableBreakdown { get; set; }
        [JsonProperty("links")]
        public List<LinksCapture> Links { get; set; }
    }
    public class AmountCapture {
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }
    public class SellerProtectionCapture {
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("dispute_categories")]
        public List<string> DisputeCategories { get; set; } = new();

    }
    public class SellerReceivableBreakdownCapture
    {
        [JsonProperty("gross_amount")]
        public GrossAmount Amount { get; set; }
        [JsonProperty("paypal_fee")]
        public PaypalFee PaypalFee { get; set; }
        [JsonProperty("net_amount")]
        public NetAmount NetAmount { get; set; }
        [JsonProperty("create_time")]
        public string CreateTime { get; set; }
        [JsonProperty("update_time")]
        public string UpdateTime { get; set; }
    }
    public class GrossAmount {
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }
    public class PaypalFee {
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }
    public class NetAmount {

        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }
    public class LinksCapture
    {
        [JsonProperty("href")]
        public string Href { get; set; }
        [JsonProperty("rel")]
        public string Rel { get; set; }
        [JsonProperty("method")]
        public string Method { get; set; }
    }
    public class PayerCapture
    {
        [JsonProperty("name")]
        public NameCapture Name { get; set; }
        [JsonProperty("email_address")]
        public string EmailAddress { get; set; }
        [JsonProperty("payer_id")]
        public string  PayerId { get; set; }
    }
}
