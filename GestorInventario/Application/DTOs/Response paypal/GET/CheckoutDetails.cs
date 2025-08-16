using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal.GET
{
    public class CheckoutDetails
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("intent")]
        public string Intent { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("payment_source")]
        public PaymentSourceCheckout PaymentSource { get; set; }

        [JsonProperty("purchase_units")]
        public List<PurchaseUnitsBse> PurchaseUnits { get; set; }

        [JsonProperty("payer")]
        public Payer Payer { get; set; }

        [JsonProperty("create_time")]
        public string CreateTime { get; set; }
        [JsonProperty("update_time")]
        public string UpdateTime { get; set; }

        [JsonProperty("links")]
        public List<Link> Links { get; set; }
    }

    public class PaymentSourceCheckout
    {
        [JsonProperty("paypal")]
        public PayPal Paypal { get; set; }
    }

    public class PayPal
    {

        [JsonProperty("email_address")]
        public string Email { get; set; }

        [JsonProperty("account_id")]
        public string AccountId { get; set; }
        [JsonProperty("account_status")]
        public string AccountStatus { get; set; }
        [JsonProperty("name")]
        public NameDetails Name { get; set; }

        [JsonProperty("address")]
        public AddressBse Address { get; set; }
    }
    public class NameDetails
    {
        [JsonProperty("given_name")]
        public string GivenName { get; set; }
        [JsonProperty("surname")]
        public string Surname { get; set; }
    }

    public class AddressBse
    {
        [JsonProperty("country_code")]
        public string CountryCode { get; set; }
    }

    public class PurchaseUnitsBse
    {
        [JsonProperty("reference_id")]
        public string ReferenceId { get; set; }
        [JsonProperty("amount")]
        public AmountBse Amount { get; set; }
        [JsonProperty("payee")]
        public Payee Payee { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("invoice_id")]
        public string InvoiceId { get; set; }
        [JsonProperty("items")]
        public List<ItemBse> Items { get; set; }

        [JsonProperty("shipping")]
        public ShippingBse Shipping { get; set; }

        [JsonProperty("payments")]
        public Payments Payments { get; set; }

      

      
    }

    public class AmountBse
    {
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("breakdown")]
        public BreakdownBse Breakdown { get; set; }


    }

    public class BreakdownBse
    {
        [JsonProperty("item_total")]
        public MoneyBse ItemTotal { get; set; }

        [JsonProperty("shipping")]
        public MoneyBse Shipping { get; set; }

        [JsonProperty("handling")]
        public MoneyBse Handling { get; set; }
        [JsonProperty("tax_total")]
        public MoneyBse TaxTotal { get; set; }
        [JsonProperty("insurance")]
        public MoneyBse Insurance { get; set; }
        [JsonProperty("shipping_discount")]
        public MoneyBse ShippingDiscount { get; set; }
    }

    public class MoneyBse
    {
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class ItemBse
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("unit_amount")]
        public MoneyBse UnitAmount { get; set; }
        [JsonProperty("tax")]
        public MoneyBse Tax { get; set; }
        [JsonProperty("quantity")]
        public string Quantity { get; set; }
        [JsonProperty("sku")]

        public string Sku { get; set; }
      
    }

    public class Payee
    {
        [JsonProperty("merchant_id")]
        public string MerchantId { get; set; }

        [JsonProperty("email_address")]
        public string EmailAddress { get; set; }
    }

    public class Payments
    {
        [JsonProperty("captures")]
        public List<Capture> Captures { get; set; }

        [JsonProperty("refunds")]
        public List<Refund> Refunds { get; set; }
    }

    public class Capture
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("amount")]
        public MoneyBse Amount { get; set; }

        [JsonProperty("final_capture")]
        public bool FinalCapture { get; set; }

        [JsonProperty("seller_protection")]
        public SellerProtection SellerProtection { get; set; }

        [JsonProperty("seller_receivable_breakdown")]
        public SellerReceivableBreakdown SellerReceivableBreakdown { get; set; }

        [JsonProperty("invoice_id")]
        public string InvoiceId { get; set; }

        [JsonProperty("links")]
        public List<Link> Links { get; set; }

        [JsonProperty("create_time")]
        public string CreateTime { get; set; }

        [JsonProperty("update_time")]
        public string UpdateTime { get; set; }
    }

    public class SellerProtection
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("dispute_categories")]
        public List<string> DisputeCategories { get; set; }
    }

    public class SellerReceivableBreakdown
    {
        [JsonProperty("gross_amount")]
        public MoneyBse GrossAmount { get; set; }

        [JsonProperty("paypal_fee")]
        public MoneyBse PaypalFee { get; set; }

        [JsonProperty("net_amount")]
        public MoneyBse NetAmount { get; set; }

        [JsonProperty("exchange_rate")]
        public ExchangeRate ExchangeRate { get; set; }
    }

    public class ExchangeRate
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class Refund
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("amount")]
        public MoneyBse Amount { get; set; }

        [JsonProperty("note_to_payer")]
        public string NoteToPayer { get; set; }

        [JsonProperty("seller_payable_breakdown")]
        public SellerPayableBreakdown SellerPayableBreakdown { get; set; }

        [JsonProperty("invoice_id")]
        public string InvoiceId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("links")]
        public List<Link> Links { get; set; }

        [JsonProperty("create_time")]
        public string CreateTime { get; set; }

        [JsonProperty("update_time")]
        public string UpdateTime { get; set; }
    }

    public class SellerPayableBreakdown
    {
        [JsonProperty("gross_amount")]
        public MoneyBse GrossAmount { get; set; }

        [JsonProperty("paypal_fee")]
        public MoneyBse PaypalFee { get; set; }

        [JsonProperty("platform_fees")]
        public List<PlatformFee> PlatformFees { get; set; }

        [JsonProperty("net_amount")]
        public MoneyBse NetAmount { get; set; }

        [JsonProperty("total_refunded_amount")]
        public MoneyBse TotalRefundedAmount { get; set; }

        [JsonProperty("exchange_rate")]
        public ExchangeRate ExchangeRate { get; set; }
    }

    public class PlatformFee
    {
        [JsonProperty("amount")]
        public MoneyBse Amount { get; set; }
    }

    public class ShippingBse
    {
        [JsonProperty("name")]
        public ShippingName Name { get; set; }

        [JsonProperty("address")]
        public ShippingAddress Address { get; set; }

        [JsonProperty("trackers")]
        public List<Tracker> Trackers { get; set; }
    }

    public class ShippingName
    {
        [JsonProperty("full_name")]
        public string FullName { get; set; }
    }

    public class ShippingAddress
    {
        [JsonProperty("address_line_1")]
        public string AddressLine1 { get; set; }

        [JsonProperty("admin_area_1")]
        public string AdminArea1 { get; set; }

        [JsonProperty("admin_area_2")]
        public string AdminArea2 { get; set; }

        [JsonProperty("postal_code")]
        public string PostalCode { get; set; }

        [JsonProperty("country_code")]
        public string CountryCode { get; set; }
    }

    public class Tracker
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("items")]
        public List<TrackerItem> Items { get; set; }

        [JsonProperty("links")]
        public List<Link> Links { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("notify_payer")]
        public string NotifyPayer { get; set; }
    }

    public class TrackerItem
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("sku")]
        public string Sku { get; set; }

        [JsonProperty("quantity")]
        public string Quantity { get; set; }

        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }

        [JsonProperty("upc")]
        public UpcBse Upc { get; set; }
    }

    public class UpcBse
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
    }

    public class Payer
    {
        [JsonProperty("name")]
        public NameBse Name { get; set; }

        [JsonProperty("email_address")]
        public string Email { get; set; }

        [JsonProperty("payer_id")]
        public string PayerId { get; set; }

        [JsonProperty("address")]
        public AddressBse Address { get; set; }
    }

    public class NameBse
    {
        [JsonProperty("given_name")]
        public string GivenName { get; set; }

        [JsonProperty("surname")]
        public string Surname { get; set; }
    }

    public class Link
    {
        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("rel")]
        public string Rel { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }
    }
}