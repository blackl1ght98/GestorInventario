using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.Pedidos
{
    public class EditPedidoViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage ="El estado del pedido es requerido")]
        [Display(Name ="Estado Pedido")]
        public required string EstadoPedido { get; set; }
      
    }
}
