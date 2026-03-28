using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.DTOs.Response_paypal.POST
{
    public class CreateOrderRequest
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
        public required Amount Amount { get; set; }

        [JsonProperty("description")]
        public required string Description { get; set; }

        [JsonProperty("invoice_id")]
        public required string InvoiceId { get; set; }

        [JsonProperty("items")]
        public required List<Item> Items { get; set; } = new List<Item>();

        [JsonProperty("shipping")]
        public required Shipping Shipping { get; set; }
    }

    public class Amount
    {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; set; }

        [JsonProperty("value")]
        public required string Value { get; set; }

        [JsonProperty("breakdown")]
        public required AmountBreakdown Breakdown { get; set; }
    }

    public class AmountBreakdown
    {
        [JsonProperty("item_total")]
        public required Money ItemTotal { get; set; }

        [JsonProperty("tax_total")]
        public required Money TaxTotal { get; set; }

        [JsonProperty("shipping")]
        public required Money ShippingAmount { get; set; }
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
        public required Money UnitAmount { get; set; }

        [JsonProperty("tax")]
        public required Money Tax { get; set; }

        [JsonProperty("sku")]
        public required string Sku { get; set; }
    }

    public class Shipping
    {
        [JsonProperty("name")]
        public required OrderShippingName Name { get; set; }

        [JsonProperty("address")]
        public required OrderShippingAddress Address { get; set; }
    }

    public class OrderShippingName
    {
        [JsonProperty("full_name")]
        public required string FullName { get; set; }
    }

    public class OrderShippingAddress
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
    public class Money
    {
        [JsonProperty("currency_code")]
        [Required(ErrorMessage = "El código de moneda es requerido.")]
        public required string CurrencyCode { get; set; }

        [JsonProperty("value")]
        [Required(ErrorMessage = "El valor del monto es requerido.")]
        public required string Value { get; set; }
    }
}
