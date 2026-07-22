using GestorInventario.Domain.enums.Pedido;
using System.Text.Json.Serialization;

namespace GestorInventario.Shared.DTOS.Paypal.BD
{
    public class InfoEnvioDTO
    {
        public int PedidoId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Carrier Carrier { get; set; }

      
    }
}