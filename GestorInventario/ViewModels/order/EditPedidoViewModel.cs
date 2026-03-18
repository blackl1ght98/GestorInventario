using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.order
{
    public class EditPedidoViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage ="La fecha del pedido es requerida")]
        [Display(Name ="Fecha Pedido")]
        public DateTime FechaPedido { get; set; }
        [Required(ErrorMessage ="El estado del pedido es requerido")]
        [Display(Name ="Estado Pedido")]
        public required string EstadoPedido { get; set; }
      
    }
}
