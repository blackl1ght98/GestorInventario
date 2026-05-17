using GestorInventario.Domain.Models;

namespace GestorInventario.ViewModels.Pedidos
{
    public class PedidoDeleteViewmodel
    {
        public int Id { get; set; }
        public string NumeroPedido { get; set; }
        public DateTime FechaPedido { get; set; }
        public string NombreCompleto { get; set; }
        public string EstadoPedido { get; set; }
        public List<DetallePedido> DetallePedidos { get; set; }
    }
}
