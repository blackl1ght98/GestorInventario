using Newtonsoft.Json;

namespace GestorInventario.Application.DTOs.Response_paypal.PATCH
{
    public class PatchOperation
    {
        [JsonProperty("op")]
        public string Operation { get; set; } // Ejemplo: "replace", "add", "remove"

        [JsonProperty("path")]
        public string Path { get; set; } // Ejemplo: "/description", "/name"

        [JsonProperty("value")]
        public object Value { get; set; } // El valor a aplicar, puede ser string, object, etc.

        [JsonProperty("from", NullValueHandling = NullValueHandling.Ignore)]
        public string From { get; set; } // Opcional, usado en operaciones como "move" o "copy"
    }
}
