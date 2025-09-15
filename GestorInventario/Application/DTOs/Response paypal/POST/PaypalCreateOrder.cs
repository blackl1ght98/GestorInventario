using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.DTOs.Response_paypal.POST
{
    public class PaypalCreateOrder
    {
        [JsonProperty("intent")]
        public required string Intent { get; set; }

        [JsonProperty("purchase_units")]
        public required List<PurchaseUnit> PurchaseUnits { get; set; } = new List<PurchaseUnit>();

        [JsonProperty("payment_source")]
        public required PaymentSource PaymentSource { get; set; }
    } 

    public class PurchaseUnit
    {
        [JsonProperty("amount")]
        public required AmountBase Amount { get; set; }

        [JsonProperty("description")]
        public required string Description { get; set; }

        [JsonProperty("invoice_id")]
        public required string InvoiceId { get; set; }

        [JsonProperty("items")]
        public required List<Item> Items { get; set; } = new List<Item>();

        [JsonProperty("shipping")]
        public required Shipping Shipping { get; set; }
    }

    public class AmountBase
    {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; set; }

        [JsonProperty("value")]
        public required string Value { get; set; }

        [JsonProperty("breakdown")]
        public required Breakdown Breakdown { get; set; }
    }

    public class Breakdown
    {
        [JsonProperty("item_total")]
        public required MoneyOrder ItemTotal { get; set; }

        [JsonProperty("tax_total")]
        public required MoneyOrder TaxTotal { get; set; }

        [JsonProperty("shipping")]
        public required MoneyOrder ShippingAmount { get; set; }
    }



    public class Item
    {
        [JsonProperty("name")]
        public required string Name { get; set; }

        [JsonProperty("description")]
        public required string Description { get; set; }

        [JsonProperty("quantity")]
        public required string Quantity { get; set; }

        [JsonProperty("unit_amount")]
        public required MoneyOrder UnitAmount { get; set; }

        [JsonProperty("tax")]
        public required MoneyOrder Tax { get; set; }

        [JsonProperty("sku")]
        public required string Sku { get; set; }
    }

    public class Shipping
    {
        [JsonProperty("name")]
        public required NameClientOrder Name { get; set; }

        [JsonProperty("address")]
        public required Address Address { get; set; }
    }

    public class NameClientOrder
    {
        [JsonProperty("full_name")]
        public required string FullName { get; set; }
    }

    public class Address
    {
        [JsonProperty("address_line_1")]
        public required string AddressLine1 { get; set; }

        [JsonProperty("address_line_2")]
        public required string AddressLine2 { get; set; }

        [JsonProperty("admin_area_2")]
        public required string City { get; set; }

        [JsonProperty("admin_area_1")]
        public required string State { get; set; }

        [JsonProperty("postal_code")]
        public required string PostalCode { get; set; }

        [JsonProperty("country_code")]
        public required string CountryCode { get; set; }
    }

    public class PaymentSource
    {
        [JsonProperty("paypal")]
        public required Paypal Paypal { get; set; }
    }

    public class Paypal
    {
        [JsonProperty("experience_context")]
        public required ExperienceContext ExperienceContext { get; set; }
    }

    public class ExperienceContext
    {
        [JsonProperty("payment_method_preference")]
        public required string PaymentMethodPreference { get; set; }

        [JsonProperty("return_url")]
        public required string ReturnUrl { get; set; }

        [JsonProperty("cancel_url")]
        public required string CancelUrl { get; set; }
    }
    public class MoneyOrder
    {
        [JsonProperty("currency_code")]
        [Required(ErrorMessage = "El código de moneda es requerido.")]
        public required string CurrencyCode { get; set; }

        [JsonProperty("value")]
        [Required(ErrorMessage = "El valor del monto es requerido.")]
        public required string Value { get; set; }
    }
}
