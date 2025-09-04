using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal
{
    public class CreateProductResponseDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Nombre { get; set; }

        [JsonProperty("description")]
        public string Descripcion { get; set; }

        [JsonProperty("type")]
        public string Tipo { get; set; }

        [JsonProperty("category")]
        public string Categoria { get; set; }

        [JsonProperty("image_url")]
        public string Imagen { get; set; }

        [JsonProperty("home_url")]
        public string UrlInicio { get; set; } // Opcional

        [JsonProperty("create_time")]
        public string FechaCreacion { get; set; }

        [JsonProperty("update_time")]
        public string FechaActualizacion { get; set; }

        [JsonProperty("links")]
        public List<PaypalLinkDto> Enlaces { get; set; }
    }

    public class PaypalLinkDto
    {
        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("rel")]
        public string Rel { get; set; }

        [JsonProperty("method")]
        public string Metodo { get; set; }
    }
}
