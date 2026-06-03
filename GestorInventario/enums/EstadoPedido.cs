using System.ComponentModel.DataAnnotations;

namespace GestorInventario.enums
{
    public enum EstadoPedido
    {
        [Display(Name = "Entregado")]
        Entregado,

        [Display(Name = "Pagado")]
        Pagado,

        [Display(Name = "En Proceso")]
        En_Proceso,
        [Display(Name = "Pendiente de pago")]
        Pendiente,

        [Display(Name = "Rembolsado")]
        Rembolsado,
        [Display(Name = "Carrito")]
        Carrito
    }
}