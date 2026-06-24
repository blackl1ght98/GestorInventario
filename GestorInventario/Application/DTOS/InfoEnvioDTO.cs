using GestorInventario.enums.Pedido;
using GestorInventario.enums.Productos;
using System.Text.Json.Serialization;

namespace GestorInventario.Application.DTOS
{
    public class InfoEnvioDTO
    {
        public int PedidoId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Carrier Carrier { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BarcodeType Barcode { get; set; }
    }
}