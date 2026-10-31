using System.Text.Json.Serialization;
using GestorInventario.Domain.enums.Pedido;

namespace GestorInventario.Application.DTOS.Paypal
{
    public record InfoEnvioDto
    {
        public int PedidoId { get; init; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Carrier Carrier { get; init; }

      
    }
}