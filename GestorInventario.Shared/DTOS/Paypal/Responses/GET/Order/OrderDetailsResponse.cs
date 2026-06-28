using Newtonsoft.Json;

namespace GestorInventario.Shared.DTOS.Paypal.Responses.GET.Order
{
    public record OrderDetailsResponse
    {
        [JsonProperty("id")]
        public required string Id { get; init; }
        [JsonProperty("intent")]
        public required string Intent { get; init; }

        [JsonProperty("status")]
        public required string Status { get; init; }

        [JsonProperty("payment_source")]
        public required PaymentSourceDetails PaymentSource { get; init; }

        [JsonProperty("purchase_units")]
        public required List<PurchaseUnitDetails> PurchaseUnits { get; init; }

        [JsonProperty("payer")]
        public required Payer Payer { get; init; }

        [JsonProperty("create_time")]
        public required string CreateTime { get; init; }
        [JsonProperty("update_time")]
        public required string UpdateTime { get; init; }

        [JsonProperty("links")]
        public required List<Link> Links { get; init; }
    }

    public record PaymentSourceDetails
    {
        [JsonProperty("paypal")]
        public required PayPalDetails Paypal { get; init; }
    }

    public record PayPalDetails
    {

        [JsonProperty("email_address")]
        public required string Email { get; init; }

        [JsonProperty("account_id")]
        public required string AccountId { get; init; }
        [JsonProperty("account_status")]
        public required string AccountStatus { get; init; }
        [JsonProperty("name")]
        public required NameDetails Name { get; init; }

        [JsonProperty("address")]
        public required AddressDetails Address { get; init; }
    }
    public record NameDetails
    {
        [JsonProperty("given_name")]
        public required string GivenName { get; init; }
        [JsonProperty("surname")]
        public required string Surname { get; init; }
    }

    public record AddressDetails
    {
        [JsonProperty("country_code")]
        public required string CountryCode { get; init; }
    }

    public record PurchaseUnitDetails
    {
        [JsonProperty("reference_id")]
        public required string ReferenceId { get; init; }
        [JsonProperty("amount")]
        public required AmountDetails Amount { get; init; }
        [JsonProperty("payee")]
        public required PayeeDetails Payee { get; init; }
        [JsonProperty("description")]
        public required string Description { get; init; }
        [JsonProperty("invoice_id")]
        public required string InvoiceId { get; init; }
        [JsonProperty("items")]
        public required List<ItemDetails> Items { get; init; }

        [JsonProperty("shipping")]
        public required ShippingDetails Shipping { get; init; }

        [JsonProperty("payments")]
        public required PaymentsDetails Payments { get; init; }     
    }

    public record AmountDetails
    {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; init; }

        [JsonProperty("value")]
        public required string Value { get; init; }

        [JsonProperty("breakdown")]
        public required BreakdownDetails Breakdown { get; init; }
    }

    public record BreakdownDetails
    {
        [JsonProperty("item_total")]
        public required MoneyDetails ItemTotal { get; init; }

        [JsonProperty("shipping")]
        public required MoneyDetails Shipping { get; init; }

        [JsonProperty("handling")]
        public required MoneyDetails Handling { get; init; }
        [JsonProperty("tax_total")]
        public required MoneyDetails TaxTotal { get; init; }
        [JsonProperty("insurance")]
        public required MoneyDetails Insurance { get; init; }
        [JsonProperty("shipping_discount")]
        public required MoneyDetails ShippingDiscount { get; init; }
    }

    public record MoneyDetails
    {
        [JsonProperty("currency_code")]
        public required string CurrencyCode { get; init; }

        [JsonProperty("value")]
        public required string Value { get; init; }
    }

    public record ItemDetails
    {
        [JsonProperty("name")]
        public required string Name { get; init; }
        [JsonProperty("unit_amount")]
        public required MoneyDetails UnitAmount { get; init; }
        [JsonProperty("tax")]
        public required MoneyDetails Tax { get; init; }
        [JsonProperty("quantity")]
        public required string Quantity { get; init; }
        [JsonProperty("sku")]
        public required string Sku { get; init; }
      
    }

    public record PayeeDetails
    {
        [JsonProperty("merchant_id")]
        public required string MerchantId { get; init; }

        [JsonProperty("email_address")]
        public required string EmailAddress { get; init; }
    }

    public record PaymentsDetails
    {
        [JsonProperty("captures")]
        public required List<CaptureDetails> Captures { get; init; }

        [JsonProperty("refunds")]
        public required List<RefundDetails> Refunds { get; init; }
    }

    public record CaptureDetails
    {
        [JsonProperty("id")]
        public required string Id { get; init; }

        [JsonProperty("status")]
        public required string Status { get; init; }

        [JsonProperty("amount")]
        public required MoneyDetails Amount { get; init; }

        [JsonProperty("final_capture")]
        public required bool FinalCapture { get; init; }

        [JsonProperty("seller_protection")]
        public required SellerProtection SellerProtection { get; init; }

        [JsonProperty("seller_receivable_breakdown")]
        public required SellerReceivableBreakdown SellerReceivableBreakdown { get; init; }

        [JsonProperty("invoice_id")]
        public required string InvoiceId { get; init; }

        [JsonProperty("links")]
        public required List<Link> Links { get; init; }

        [JsonProperty("create_time")]
        public required string CreateTime { get; init; }

        [JsonProperty("update_time")]
        public required string UpdateTime { get; init; }
    }

    public record SellerProtection
    {
        [JsonProperty("status")]
        public required string Status { get; init; }

        [JsonProperty("dispute_categories")]
        public required List<string> DisputeCategories { get; init; }
    }

    public record SellerReceivableBreakdown
    {
        [JsonProperty("gross_amount")]
        public required MoneyDetails GrossAmount { get; init; }

        [JsonProperty("paypal_fee")]
        public required MoneyDetails PaypalFee { get; init; }

        [JsonProperty("net_amount")]
        public required MoneyDetails NetAmount { get; init; }

        [JsonProperty("exchange_rate")]
        public ExchangeRate? ExchangeRate { get; init; }
      
    }

    public record ExchangeRate
    {
        [JsonProperty("value")]
        public string? Value { get; init; }
    }

    public record RefundDetails
    {
        [JsonProperty("id")]
        public required string Id { get; init; }

        [JsonProperty("amount")]
        public required MoneyDetails Amount { get; init; }

        [JsonProperty("note_to_payer")]
        public required string NoteToPayer { get; init; }

        [JsonProperty("seller_payable_breakdown")]
        public required SellerPayableBreakdown SellerPayableBreakdown { get; init; }

        [JsonProperty("invoice_id")]
        public required string InvoiceId { get; init; }

        [JsonProperty("status")]
        public required string Status { get; init; }

        [JsonProperty("links")]
        public required List<Link> Links { get; init; }

        [JsonProperty("create_time")]
        public required string CreateTime { get; init; }

        [JsonProperty("update_time")]
        public required string UpdateTime { get; init; }
    }

    public record SellerPayableBreakdown
    {
        [JsonProperty("gross_amount")]
        public required MoneyDetails GrossAmount { get; init; }

        [JsonProperty("paypal_fee")]
        public required MoneyDetails PaypalFee { get; init; }

        [JsonProperty("platform_fees")]
        public required List<PlatformFee> PlatformFees { get; init; }

        [JsonProperty("net_amount")]
        public required MoneyDetails NetAmount { get; init; }

        [JsonProperty("total_refunded_amount")]
        public required MoneyDetails TotalRefundedAmount { get; init; }

        [JsonProperty("exchange_rate")]
        public ExchangeRate? ExchangeRate { get; init; }
    }    
    public record PlatformFee
    {
        [JsonProperty("amount")]
        public required MoneyDetails Amount { get; init; }
    }

    public record ShippingDetails
    {
        [JsonProperty("name")]
        public required ShippingName Name { get; init; }

        [JsonProperty("address")]
        public required ShippingAddress Address { get; init; }

        [JsonProperty("trackers")]
        public required List<Tracker> Trackers { get; init; }
    }

    public record ShippingName
    {
        [JsonProperty("full_name")]
        public required string FullName { get; init; }
    }

    public record ShippingAddress
    {
        [JsonProperty("address_line_1")]
        public required string AddressLine1 { get; init; }

        [JsonProperty("admin_area_1")]
        public required string AdminArea1 { get; init; }

        [JsonProperty("admin_area_2")]
        public required string AdminArea2 { get; init; }

        [JsonProperty("postal_code")]
        public required string PostalCode { get; init; }

        [JsonProperty("country_code")]
        public required string CountryCode { get; init; }
    }

    public record Tracker
    {
        [JsonProperty("id")]
        public required string Id { get; init; }

        [JsonProperty("items")]
        public required List<TrackerItem> Items { get; init; }

        [JsonProperty("links")]
        public required List<Link> Links { get; init; }

        [JsonProperty("status")]
        public required string Status { get; init; }

        [JsonProperty("notify_payer")]
        public required string NotifyPayer { get; init; }
    }

    public record TrackerItem
    {
        [JsonProperty("name")]
        public required string Name { get; init; }

        [JsonProperty("sku")]
        public required string Sku { get; init; }

        [JsonProperty("quantity")]
        public required string Quantity { get; init; }

        [JsonProperty("image_url")]
        public required string ImageUrl { get; init; }

        [JsonProperty("upc")]
        public required Upc Upc { get; init; }
    }

    public record Upc
    {
        [JsonProperty("type")]
        public required string Type { get; init; }

        [JsonProperty("code")]
        public required string Code { get; init; }
    }

    public record Payer
    {
        [JsonProperty("name")]
        public required NameDetails Name { get; init; }

        [JsonProperty("email_address")]
        public required string Email { get; init; }

        [JsonProperty("payer_id")]
        public required string PayerId { get; init; }

        [JsonProperty("address")]
        public required AddressDetails Address { get; init; }
    }

     

    public record Link
    {
        [JsonProperty("href")]
        public required string Href { get; init; }

        [JsonProperty("rel")]
        public required string Rel { get; init; }

        [JsonProperty("method")]
        public required string Method { get; init; }
    }
}