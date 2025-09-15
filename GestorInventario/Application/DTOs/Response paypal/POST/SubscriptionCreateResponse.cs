using Newtonsoft.Json;
using System.Collections.Generic;

namespace GestorInventario.Application.DTOs.Response.PayPal
{
    // DTO para la respuesta completa de la creación de una suscripción
    public class SubscriptionCreateResponse
    {
        [JsonProperty("id")]
        public  string Id { get; set; }

        [JsonProperty("plan_id")]
        public required string PlanId { get; set; }

        [JsonProperty("status")]
        public  string Status { get; set; }

        [JsonProperty("application_context")]
        public required ApplicationContext ApplicationContext { get; set; }

        [JsonProperty("links")]
        public  List<Link> Links { get; set; }
    }

    public class ApplicationContext
    {
        [JsonProperty("brand_name")]
        public required string BrandName { get; set; }

        [JsonProperty("locale")]
        public required string Locale { get; set; }

        [JsonProperty("shipping_preference")]
        public required string ShippingPreference { get; set; }

        [JsonProperty("user_action")]
        public required string UserAction { get; set; }

        [JsonProperty("payment_method")]
        public required PaymentMethod PaymentMethod { get; set; }

        [JsonProperty("return_url")]
        public required string ReturnUrl { get; set; }

        [JsonProperty("cancel_url")]
        public required string CancelUrl { get; set; }
    }

    public class PaymentMethod
    {
        [JsonProperty("payer_selected")]
        public required string PayerSelected { get; set; }

        [JsonProperty("payee_preferred")]
        public required string PayeePreferred { get; set; }
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