using Newtonsoft.Json;
using System.Collections.Generic;

namespace GestorInventario.Application.DTOs.Response_paypal.POST
{
    public class PaypalCreateOrder
    {
        [JsonProperty("intent")]
        public string Intent { get; set; }

        [JsonProperty("purchase_units")]
        public List<PurchaseUnit> PurchaseUnits { get; set; } = new List<PurchaseUnit>();

        [JsonProperty("payment_source")]
        public PaymentSource PaymentSource { get; set; }
    }

    public class PurchaseUnit
    {
        [JsonProperty("amount")]
        public AmountBase Amount { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("invoice_id")]
        public string InvoiceId { get; set; }

        [JsonProperty("items")]
        public List<Item> Items { get; set; } = new List<Item>();

        [JsonProperty("shipping")]
        public Shipping Shipping { get; set; }
    }

    public class AmountBase
    {
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("breakdown")]
        public Breakdown Breakdown { get; set; }
    }

    public class Breakdown
    {
        [JsonProperty("item_total")]
        public MoneyBase ItemTotal { get; set; }

        [JsonProperty("tax_total")]
        public MoneyBase TaxTotal { get; set; }

        [JsonProperty("shipping")]
        public MoneyBase ShippingAmount { get; set; }
    }

    public class MoneyBase
    {
        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    public class Item
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("quantity")]
        public string Quantity { get; set; }

        [JsonProperty("unit_amount")]
        public MoneyBase UnitAmount { get; set; }

        [JsonProperty("tax")]
        public MoneyBase Tax { get; set; }

        [JsonProperty("sku")]
        public string Sku { get; set; }
    }

    public class Shipping
    {
        [JsonProperty("name")]
        public NameClientOrder Name { get; set; }

        [JsonProperty("address")]
        public Address Address { get; set; }
    }

    public class NameClientOrder
    {
        [JsonProperty("full_name")]
        public string FullName { get; set; }
    }

    public class Address
    {
        [JsonProperty("address_line_1")]
        public string AddressLine1 { get; set; }

        [JsonProperty("address_line_2")]
        public string AddressLine2 { get; set; }

        [JsonProperty("admin_area_2")]
        public string City { get; set; }

        [JsonProperty("admin_area_1")]
        public string State { get; set; }

        [JsonProperty("postal_code")]
        public string PostalCode { get; set; }

        [JsonProperty("country_code")]
        public string CountryCode { get; set; }
    }

    public class PaymentSource
    {
        [JsonProperty("paypal")]
        public Paypal Paypal { get; set; }
    }

    public class Paypal
    {
        [JsonProperty("experience_context")]
        public ExperienceContext ExperienceContext { get; set; }
    }

    public class ExperienceContext
    {
        [JsonProperty("payment_method_preference")]
        public string PaymentMethodPreference { get; set; }

        [JsonProperty("return_url")]
        public string ReturnUrl { get; set; }

        [JsonProperty("cancel_url")]
        public string CancelUrl { get; set; }
    }
}
