using GestorInventario.Domain.Models;

namespace GestorInventario.ViewModels.Orders
{
    public class OrderDetailsViewmodel
    {
        public DateTime FechaPedido { get; set; }
        public string NombreCompleto { get; set; }
        public string TrackingNumber { get; set; }
        public string Transportista { get; set; }
        public string NumeroPedido { get; set; }
        public string EstadoPedido { get; set; }
        public string Currency { get; set; }
        public List<DetallePedido> DetallePedidos { get; set; }
    }
}
