using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.order
{
    public class EditPedidoViewModel
    {
        public int Id { get; set; }
        public DateTime FechaPedido { get; set; }
        public required string EstadoPedido { get; set; }
      
    }
}
