using Newtonsoft.Json;

using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Shared.DTOS.Paypal.Requests.POST
{
    /**
   * DTO de solicitud para la creación de órdenes en PayPal.
   * Esta clase y sus dependencias representan la jerarquía de datos necesaria para:
   * - Configurar el flujo de pago (Intent y PaymentSource).
   * - Detallar los artículos, cantidades y precios (PurchaseUnits e Items).
   * - Gestionar el desglose de impuestos y envío (AmountBreakdown y Shipping).
   *
   * Cumple con la especificación de la API de PayPal para la creación de órdenes.
   */
    public record CreateOrderRequest
    {
        [JsonProperty("intent")]
        public required string Intent { get; init; }

        [JsonProperty("purchase_units")]
        public required List<PurchaseUnit> PurchaseUnits { get; init; } = new();

        [JsonProperty("payment_source")]
        public required PaymentSource PaymentSource { get; init; }
    }

    public record PurchaseUnit
    {
        [JsonProperty("amount")]
        public required Amount Amount { get; init; }

        [JsonProperty("description")]
        public required string Description { get; init; }

        [JsonProperty("invoice_id")]
        public required string InvoiceId { get; init; }

        [JsonProperty("items")]
        public required List<Item> Items { get; init; } = new();

        [JsonProperty("shipping")]
        public required Shipping Shipping { get; init; }
    }

    public record Amount
    {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; init; }

        [JsonProperty("value")]
        public required string Value { get; init; }

        [JsonProperty("breakdown")]
        public required AmountBreakdown Breakdown { get; init; }
    }

    public record AmountBreakdown
    {
        [JsonProperty("item_total")]
        public required Money ItemTotal { get; init; }

        [JsonProperty("tax_total")]
        public required Money TaxTotal { get; init; }

        [JsonProperty("shipping")]
        public required Money ShippingAmount { get; init; }
    }

    public record Item
    {
        [JsonProperty("name")]
        public required string Name { get; init; }

        [JsonProperty("description")]
        public required string Description { get; init; }

        [JsonProperty("quantity")]
        public required string Quantity { get; init; }

        [JsonProperty("unit_amount")]
        public required Money UnitAmount { get; init; }

        [JsonProperty("tax")]
        public required Money Tax { get; init; }

        [JsonProperty("sku")]
        public required string Sku { get; init; }
    }

    public record Shipping
    {
        [JsonProperty("name")]
        public required OrderShippingName Name { get; init; }

        [JsonProperty("address")]
        public required OrderShippingAddress Address { get; init; }
    }

    public record OrderShippingName
    {
        [JsonProperty("full_name")]
        public required string FullName { get; init; }
    }

    public record OrderShippingAddress
    {
        [JsonProperty("address_line_1")]
        public required string AddressLine1 { get; init; }

        [JsonProperty("address_line_2")]
        public required string AddressLine2 { get; init; }

        [JsonProperty("admin_area_2")]
        public required string City { get; init; }

        [JsonProperty("admin_area_1")]
        public required string State { get; init; }

        [JsonProperty("postal_code")]
        public required string PostalCode { get; init; }

        [JsonProperty("country_code")]
        public required string CountryCode { get; init; }
    }

    public record PaymentSource
    {
        [JsonProperty("paypal")]
        public required Paypal Paypal { get; init; }
    }

    public record Paypal
    {
        [JsonProperty("experience_context")]
        public required ExperienceContext ExperienceContext { get; init; }
    }

    public record ExperienceContext
    {
        [JsonProperty("payment_method_preference")]
        public required string PaymentMethodPreference { get; init; }

        [JsonProperty("return_url")]
        public required string ReturnUrl { get; init; }

        [JsonProperty("cancel_url")]
        public required string CancelUrl { get; init; }
    }

    public record Money
    {
        [JsonProperty("currency_code")]
        [Required(ErrorMessage = "El código de moneda es requerido.")]
        public required string CurrencyCode { get; init; }

        [JsonProperty("value")]
        [Required(ErrorMessage = "El valor del monto es requerido.")]
        public required string Value { get; init; }
    }
}
