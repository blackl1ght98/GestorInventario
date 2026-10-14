using Newtonsoft.Json;

namespace GestorInventario.Shared.ExternalServices.Paypal.API.Requests.PATCH
{
    public record PatchOperationRequest
    {
        [JsonProperty("op")]
        public  string? Operation { get; init; } // Ejemplo: "replace", "add", "remove"

        [JsonProperty("path")]
        public  string? Path { get; init; } // Ejemplo: "/description", "/name"

        [JsonProperty("value")]
        public   object? Value { get; init; } // El valor a aplicar, puede ser string, object, etc.


    }
}
