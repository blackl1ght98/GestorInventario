using System.Text.Json.Serialization;
using GestorInventario.Domain.enums.Pedido;

namespace GestorInventario.Application.DTOS.Paypal
{
    public class InfoEnvioDto
    {
        public int PedidoId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Carrier Carrier { get; set; }

      
    }
}