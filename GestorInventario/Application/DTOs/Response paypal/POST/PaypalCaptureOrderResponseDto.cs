using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal.POST
{
    public class PaypalCaptureOrderResponseDto
    {
        [JsonProperty("id")]
        public required string Id { get; set; }
        [JsonProperty("status")]
        public required string Status { get; set; }
        [JsonProperty("payment_source")]
        public required PaymentSourceCapture PaymentSource { get; set; }
        [JsonProperty("purchase_units")]
        public required List<PurchaseUnitsCapture> PurchaseUnits { get; set; }
        [JsonProperty("payer")]
        public required PayerCapture PayerCapture { get; set; }
        [JsonProperty("links")]
        public required List<LinksCapture> Links { get; set; }
    }
    public class PaymentSourceCapture
    {
        [JsonProperty("paypal")]
        public required PaypalCapture Paypal { get; set; }
    }
    public class PaypalCapture
    {
        [JsonProperty("name")]
        public required NameCapture Name { get; set; }
        [JsonProperty("email_address")]
        public required string EmailAddress { get; set; }
        [JsonProperty("account_id")]
        public required string AccountId { get; set; }
    }
    public class NameCapture {
        [JsonProperty("given_name")]
        public required string GivenName { get; set; }
        [JsonProperty("surname")]
        public required string Surname { get; set; }

    }
    public class PurchaseUnitsCapture
    {
        [JsonProperty("reference_id")]
        public required string ReferenceId { get; set; }
        [JsonProperty("shipping")]
        public required ShippingCapture Shipping { get; set; }
        [JsonProperty("payments")]
        public required PaymentsCapture Payments { get; set; }
    }
    public class ShippingCapture {
        [JsonProperty("address")]
        public required AddressCapture Address { get; set; }
    
    }
    public class AddressCapture {
        [JsonProperty("address_line_1")]
        public required string AddressLine1 { get; set; }
        [JsonProperty("address_line_2")]
        public required string AddressLine2 { get; set; }
        [JsonProperty("admin_area_2")]
        public required string AdminArea2 { get; set; }
        [JsonProperty("admin_area_1")]
        public required string AdminArea1 { get; set; }
        [JsonProperty("postal_code")]
        public required string PostalCode { get; set; }
        [JsonProperty("country_code")]
        public required string CountryCode { get; set; }
    }
    public class PaymentsCapture
    {
        [JsonProperty("captures")]
        public required List<Captures> Captures { get; set; }
    }
    public class Captures
    {
        [JsonProperty("id")]
        public required string Id { get; set; }
        [JsonProperty("status")]
        public required string Status { get; set; }
        [JsonProperty("amount")]
        public required AmountCapture Amount { get; set; }
        [JsonProperty("seller_protection")]
        public required SellerProtectionCapture SellerProtection { get; set; }
        [JsonProperty("final_capture")]
        public required bool FinalCapture { get; set; }
        [JsonProperty("disbursement_mode")]
        public required string DisbursementMode { get; set; }
        [JsonProperty("seller_receivable_breakdown")]
        public required SellerReceivableBreakdownCapture SellerReceivableBreakdown { get; set; }
        [JsonProperty("links")]
        public required List<LinksCapture> Links { get; set; }
    }
    public class AmountCapture {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; set; }
        [JsonProperty("value")]
        public required string Value { get; set; }
    }
    public class SellerProtectionCapture {
        [JsonProperty("status")]
        public required string Status { get; set; }
        [JsonProperty("dispute_categories")]
        public required List<string> DisputeCategories { get; set; } = new();

    }
    public class SellerReceivableBreakdownCapture
    {
        [JsonProperty("gross_amount")]
        public required GrossAmount Amount { get; set; }
        [JsonProperty("paypal_fee")]
        public required PaypalFee PaypalFee { get; set; }
        [JsonProperty("net_amount")]
        public required NetAmount NetAmount { get; set; }
        [JsonProperty("create_time")]
        public required string CreateTime { get; set; }
        [JsonProperty("update_time")]
        public required string UpdateTime { get; set; }
    }
    public class GrossAmount {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; set; }
        [JsonProperty("value")]
        public required string Value { get; set; }
    }
    public class PaypalFee {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; set; }
        [JsonProperty("value")]
        public required string Value { get; set; }
    }
    public class NetAmount {

        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; set; }
        [JsonProperty("value")]
        public required string Value { get; set; }
    }
    public class LinksCapture
    {
        [JsonProperty("href")]
        public required string Href { get; set; }
        [JsonProperty("rel")]
        public required string Rel { get; set; }
        [JsonProperty("method")]
        public required string Method { get; set; }
    }
    public class PayerCapture
    {
        [JsonProperty("name")]
        public required NameCapture Name { get; set; }
        [JsonProperty("email_address")]
        public required string EmailAddress { get; set; }
        [JsonProperty("payer_id")]
        public required string  PayerId { get; set; }
    }
}
