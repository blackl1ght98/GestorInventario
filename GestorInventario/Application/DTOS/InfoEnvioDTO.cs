using GestorInventario.enums;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace GestorInventario.Application.DTOS
{
    public class InfoEnvioDTO
    {

        public int PedidoId { get; set; }

        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public Carrier Carrier { get; set; }

        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public BarcodeType Barcode { get; set; }

    }
}
