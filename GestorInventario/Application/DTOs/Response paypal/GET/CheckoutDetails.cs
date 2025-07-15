using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal.GET
{
    public class CheckoutDetails
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("intent")]
        public string Intent { get; set; }

        [JsonProperty("payment_source")]
        public PaymentSourceCheckout PaymentSource { get; set; }

        [JsonProperty("purchase_units")]
        public List<PurchaseUnitsBse> PurchaseUnits { get; set; }

        [JsonProperty("payer")]
        public Payer Payer { get; set; }

        [JsonProperty("create_time")]
        public string CreateTime { get; set; }

        [JsonProperty("links")]
        public List<Link> Links { get; set; }
    }

    public class PaymentSourceCheckout
    {
        [JsonProperty("paypal")]
        public PaypalSource Paypal { get; set; }
    }

    public class PaypalSource
    {
        [JsonProperty("name")]
        public NameBse Name { get; set; }

        [JsonProperty("email_address")]
        public string Email { get; set; }

        [JsonProperty("account_id")]
        public string AccountId { get; set; }
    }

    public class PurchaseUnitsBse
    {
        [JsonProperty("reference_id")]
        public string ReferenceId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("amount")]
        public AmountBse Amount { get; set; }

        [JsonProperty("items")]
        public List<ItemBse> Items { get; set; }

        [JsonProperty("payee")]
        public Payee Payee { get; set; }

        [JsonProperty("payments")]
        public Payments Payments { get; set; }

        [JsonProperty("shipping")]
        public ShippingBse Shipping { get; set; }
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

        [JsonProperty("sku")]
        public string Sku { get; set; }

        [JsonProperty("unit_amount")]
        public MoneyBse UnitAmount { get; set; }

        [JsonProperty("tax")]
        public MoneyBse Tax { get; set; }

        [JsonProperty("quantity")]
        public string Quantity { get; set; }
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
    }

    public class Capture
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("amount")]
        public Money Amount { get; set; }

        [JsonProperty("seller_protection")]
        public SellerProtection SellerProtection { get; set; }

        [JsonProperty("seller_receivable_breakdown")]
        public SellerReceivableBreakdown SellerReceivableBreakdown { get; set; }

        [JsonProperty("create_time")]
        public string CreateTime { get; set; }

        [JsonProperty("update_time")]
        public string UpdateTime { get; set; }
    }

    public class SellerProtection
    {
        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public class SellerReceivableBreakdown
    {
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

    public class ShippingBse
    {
        [JsonProperty("name")]
        public ShippingName Name { get; set; }

        [JsonProperty("address")]
        public ShippingAddress Address { get; set; }
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

    public class Payer
    {
        [JsonProperty("name")]
        public NameBse Name { get; set; }

        [JsonProperty("email_address")]
        public string Email { get; set; }

        [JsonProperty("payer_id")]
        public string PayerId { get; set; }
    }

    public class NameBse
    {
        [JsonProperty("given_name")]
        public string GivenName { get; set; }

        [JsonProperty("surname")]
        public string Surname { get; set; }
    }
}
