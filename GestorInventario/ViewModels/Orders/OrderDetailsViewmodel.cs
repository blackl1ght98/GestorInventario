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

        public List<DetallePedidoLineaViewModel> Lineas { get; set; }

        public decimal TotalSinIva { get; set; }
        public decimal TotalIva { get; set; }
        public decimal GranTotal { get; set; }
    }
}
