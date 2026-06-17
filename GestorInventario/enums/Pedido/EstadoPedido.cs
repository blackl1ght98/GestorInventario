using System.ComponentModel.DataAnnotations;

namespace GestorInventario.enums.Pedido
{
    public enum EstadoPedido
    {
        [Display(Name = "Entregado")]
        Entregado,

        [Display(Name = "Pagado")]
        Pagado,

        Enviado,
        Cancelado,
        [Display(Name = "Pendiente de pago")]
        Pendiente,

        [Display(Name = "Rembolsado")]
        Rembolsado,
        [Display(Name = "Carrito")]
        Carrito
    }
}