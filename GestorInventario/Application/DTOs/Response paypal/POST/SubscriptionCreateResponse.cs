using Newtonsoft.Json;
using System.Collections.Generic;

namespace GestorInventario.Application.DTOs.Response.PayPal
{
    // DTO para la respuesta completa de la creación de una suscripción
    public class SubscriptionCreateResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("plan_id")]
        public string PlanId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("application_context")]
        public ApplicationContext ApplicationContext { get; set; }

        [JsonProperty("links")]
        public List<Link> Links { get; set; }
    }

    public class ApplicationContext
    {
        [JsonProperty("brand_name")]
        public string BrandName { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("shipping_preference")]
        public string ShippingPreference { get; set; }

        [JsonProperty("user_action")]
        public string UserAction { get; set; }

        [JsonProperty("payment_method")]
        public PaymentMethod PaymentMethod { get; set; }

        [JsonProperty("return_url")]
        public string ReturnUrl { get; set; }

        [JsonProperty("cancel_url")]
        public string CancelUrl { get; set; }
    }

    public class PaymentMethod
    {
        [JsonProperty("payer_selected")]
        public string PayerSelected { get; set; }

        [JsonProperty("payee_preferred")]
        public string PayeePreferred { get; set; }
    }

    // Nuevo DTO para los enlaces (links) en la respuesta
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