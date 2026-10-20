using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace GestorInventario.Infrastructure.ExternalServices.Common;

public record Money
{
    [JsonProperty("currency_code")]
    [Required(ErrorMessage = "El código de moneda es requerido.")]
    public required string CurrencyCode { get; init; }

    [JsonProperty("value")]
    [Required(ErrorMessage = "El valor del monto es requerido.")]
    public required string Value { get; init; }
}