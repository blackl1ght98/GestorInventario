using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal.GET
{
    public class OrderDetailsResponse
    {
        [JsonProperty("id")]
        public required string Id { get; set; }
        [JsonProperty("intent")]
        public required string Intent { get; set; }

        [JsonProperty("status")]
        public required string Status { get; set; }

        [JsonProperty("payment_source")]
        public required PaymentSourceDetails PaymentSource { get; set; }

        [JsonProperty("purchase_units")]
        public required List<PurchaseUnitDetails> PurchaseUnits { get; set; }

        [JsonProperty("payer")]
        public required Payer Payer { get; set; }

        [JsonProperty("create_time")]
        public required string CreateTime { get; set; }
        [JsonProperty("update_time")]
        public required string UpdateTime { get; set; }

        [JsonProperty("links")]
        public required List<Link> Links { get; set; }
    }

    public class PaymentSourceDetails
    {
        [JsonProperty("paypal")]
        public required PayPalDetails Paypal { get; set; }
    }

    public class PayPalDetails
    {

        [JsonProperty("email_address")]
        public required string Email { get; set; }

        [JsonProperty("account_id")]
        public required string AccountId { get; set; }
        [JsonProperty("account_status")]
        public required string AccountStatus { get; set; }
        [JsonProperty("name")]
        public required NameDetails Name { get; set; }

        [JsonProperty("address")]
        public required AddressDetails Address { get; set; }
    }
    public class NameDetails
    {
        [JsonProperty("given_name")]
        public required string GivenName { get; set; }
        [JsonProperty("surname")]
        public required string Surname { get; set; }
    }

    public class AddressDetails
    {
        [JsonProperty("country_code")]
        public required string CountryCode { get; set; }
    }

    public class PurchaseUnitDetails
    {
        [JsonProperty("reference_id")]
        public required string ReferenceId { get; set; }
        [JsonProperty("amount")]
        public required AmountDetails Amount { get; set; }
        [JsonProperty("payee")]
        public required PayeeDetails Payee { get; set; }
        [JsonProperty("description")]
        public required string Description { get; set; }
        [JsonProperty("invoice_id")]
        public required string InvoiceId { get; set; }
        [JsonProperty("items")]
        public required List<ItemDetails> Items { get; set; }

        [JsonProperty("shipping")]
        public required ShippingDetails Shipping { get; set; }

        [JsonProperty("payments")]
        public required PaymentsDetails Payments { get; set; }     
    }

    public class AmountDetails
    {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; set; }

        [JsonProperty("value")]
        public required string Value { get; set; }

        [JsonProperty("breakdown")]
        public required BreakdownDetails Breakdown { get; set; }
    }

    public class BreakdownDetails
    {
        [JsonProperty("item_total")]
        public required MoneyDetails ItemTotal { get; set; }

        [JsonProperty("shipping")]
        public required MoneyDetails Shipping { get; set; }

        [JsonProperty("handling")]
        public required MoneyDetails Handling { get; set; }
        [JsonProperty("tax_total")]
        public required MoneyDetails TaxTotal { get; set; }
        [JsonProperty("insurance")]
        public required MoneyDetails Insurance { get; set; }
        [JsonProperty("shipping_discount")]
        public required MoneyDetails ShippingDiscount { get; set; }
    }

    public class MoneyDetails
    {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; set; }

        [JsonProperty("value")]
        public required string Value { get; set; }
    }

    public class ItemDetails
    {
        [JsonProperty("name")]
        public required string Name { get; set; }
        [JsonProperty("unit_amount")]
        public required MoneyDetails UnitAmount { get; set; }
        [JsonProperty("tax")]
        public required MoneyDetails Tax { get; set; }
        [JsonProperty("quantity")]
        public required string Quantity { get; set; }
        [JsonProperty("sku")]
        public required string Sku { get; set; }
      
    }

    public class PayeeDetails
    {
        [JsonProperty("merchant_id")]
        public required string MerchantId { get; set; }

        [JsonProperty("email_address")]
        public required string EmailAddress { get; set; }
    }

    public class PaymentsDetails
    {
        [JsonProperty("captures")]
        public required List<CaptureDetails> Captures { get; set; }

        [JsonProperty("refunds")]
        public required List<RefundDetails> Refunds { get; set; }
    }

    public class CaptureDetails
    {
        [JsonProperty("id")]
        public required string Id { get; set; }

        [JsonProperty("status")]
        public required string Status { get; set; }

        [JsonProperty("amount")]
        public required MoneyDetails Amount { get; set; }

        [JsonProperty("final_capture")]
        public required bool FinalCapture { get; set; }

        [JsonProperty("seller_protection")]
        public required SellerProtection SellerProtection { get; set; }

        [JsonProperty("seller_receivable_breakdown")]
        public required SellerReceivableBreakdown SellerReceivableBreakdown { get; set; }

        [JsonProperty("invoice_id")]
        public required string InvoiceId { get; set; }

        [JsonProperty("links")]
        public required List<Link> Links { get; set; }

        [JsonProperty("create_time")]
        public required string CreateTime { get; set; }

        [JsonProperty("update_time")]
        public required string UpdateTime { get; set; }
    }

    public class SellerProtection
    {
        [JsonProperty("status")]
        public required string Status { get; set; }

        [JsonProperty("dispute_categories")]
        public required List<string> DisputeCategories { get; set; }
    }

    public class SellerReceivableBreakdown
    {
        [JsonProperty("gross_amount")]
        public required MoneyDetails GrossAmount { get; set; }

        [JsonProperty("paypal_fee")]
        public required MoneyDetails PaypalFee { get; set; }

        [JsonProperty("net_amount")]
        public required MoneyDetails NetAmount { get; set; }

        [JsonProperty("exchange_rate")]
        public ExchangeRate? ExchangeRate { get; set; }
      
    }

    public class ExchangeRate
    {
        [JsonProperty("value")]
        public string? Value { get; set; }
    }

    public class RefundDetails
    {
        [JsonProperty("id")]
        public required string Id { get; set; }

        [JsonProperty("amount")]
        public required MoneyDetails Amount { get; set; }

        [JsonProperty("note_to_payer")]
        public required string NoteToPayer { get; set; }

        [JsonProperty("seller_payable_breakdown")]
        public required SellerPayableBreakdown SellerPayableBreakdown { get; set; }

        [JsonProperty("invoice_id")]
        public required string InvoiceId { get; set; }

        [JsonProperty("status")]
        public required string Status { get; set; }

        [JsonProperty("links")]
        public required List<Link> Links { get; set; }

        [JsonProperty("create_time")]
        public required string CreateTime { get; set; }

        [JsonProperty("update_time")]
        public required string UpdateTime { get; set; }
    }

    public class SellerPayableBreakdown
    {
        [JsonProperty("gross_amount")]
        public required MoneyDetails GrossAmount { get; set; }

        [JsonProperty("paypal_fee")]
        public required MoneyDetails PaypalFee { get; set; }

        [JsonProperty("platform_fees")]
        public required List<PlatformFee> PlatformFees { get; set; }

        [JsonProperty("net_amount")]
        public required MoneyDetails NetAmount { get; set; }

        [JsonProperty("total_refunded_amount")]
        public required MoneyDetails TotalRefundedAmount { get; set; }

        [JsonProperty("exchange_rate")]
        public ExchangeRate? ExchangeRate { get; set; }
    }    
    public class PlatformFee
    {
        [JsonProperty("amount")]
        public required MoneyDetails Amount { get; set; }
    }

    public class ShippingDetails
    {
        [JsonProperty("name")]
        public required ShippingName Name { get; set; }

        [JsonProperty("address")]
        public required ShippingAddress Address { get; set; }

        [JsonProperty("trackers")]
        public required List<Tracker> Trackers { get; set; }
    }

    public class ShippingName
    {
        [JsonProperty("full_name")]
        public required string FullName { get; set; }
    }

    public class ShippingAddress
    {
        [JsonProperty("address_line_1")]
        public required string AddressLine1 { get; set; }

        [JsonProperty("admin_area_1")]
        public required string AdminArea1 { get; set; }

        [JsonProperty("admin_area_2")]
        public required string AdminArea2 { get; set; }

        [JsonProperty("postal_code")]
        public required string PostalCode { get; set; }

        [JsonProperty("country_code")]
        public required string CountryCode { get; set; }
    }

    public class Tracker
    {
        [JsonProperty("id")]
        public required string Id { get; set; }

        [JsonProperty("items")]
        public required List<TrackerItem> Items { get; set; }

        [JsonProperty("links")]
        public required List<Link> Links { get; set; }

        [JsonProperty("status")]
        public required string Status { get; set; }

        [JsonProperty("notify_payer")]
        public required string NotifyPayer { get; set; }
    }

    public class TrackerItem
    {
        [JsonProperty("name")]
        public required string Name { get; set; }

        [JsonProperty("sku")]
        public required string Sku { get; set; }

        [JsonProperty("quantity")]
        public required string Quantity { get; set; }

        [JsonProperty("image_url")]
        public required string ImageUrl { get; set; }

        [JsonProperty("upc")]
        public required Upc Upc { get; set; }
    }

    public class Upc
    {
        [JsonProperty("type")]
        public required string Type { get; set; }

        [JsonProperty("code")]
        public required string Code { get; set; }
    }

    public class Payer
    {
        [JsonProperty("name")]
        public required NameDetails Name { get; set; }

        [JsonProperty("email_address")]
        public required string Email { get; set; }

        [JsonProperty("payer_id")]
        public required string PayerId { get; set; }

        [JsonProperty("address")]
        public required AddressDetails Address { get; set; }
    }

     

    public class Link
    {
        [JsonProperty("href")]
        public required string Href { get; set; }

        [JsonProperty("rel")]
        public required string Rel { get; set; }

        [JsonProperty("method")]
        public required string Method { get; set; }
    }
}