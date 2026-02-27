using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.Paypal
{
 
    public class RefundFormViewModel
    {
        [Required(ErrorMessage ="No ha proporcionado un numero de pedido")]
        [Display(Name ="Numero de Pedido")]
        public required string NumeroPedido { get; set; }
        [Required(ErrorMessage ="No ha proporcionado su nombre")]
        [Display(Name ="Nombre del Completo")]
        public required string NombreCliente { get; set; }
        [Required(ErrorMessage ="No ha proporcionado su email")]
        [Display(Name ="Email")]
        public required string EmailCliente { get; set; }
        [Required(ErrorMessage ="Proporcione la fecha del rembolso")]
        [Display(Name ="Fecha Rembolso")]
        public DateTime FechaRembolso { get; set; }
        [Required(ErrorMessage ="Proporcine el motivo del rembolso")]
        [Display(Name ="Motivo Rembolso")]
        public required string MotivoRembolso { get; set; }
    }
}
