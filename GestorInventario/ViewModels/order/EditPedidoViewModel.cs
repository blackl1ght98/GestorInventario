using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.order
{
    public class EditPedidoViewModel
    {
        public int id { get; set; }
        public DateTime fechaPedido { get; set; }

        public string estadoPedido { get; set; }
      
    }
}
