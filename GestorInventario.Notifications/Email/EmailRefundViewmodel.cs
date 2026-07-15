using GestorInventario.Domain.Models;

namespace GestorInventario.Notifications.Email
{
    public class EmailRefundViewmodel
    {
        public required string NumeroPedido { get; set; }
        public required string NombreCliente { get; set; }
        public required string EmailCliente { get; set; }
        public DateTime? FechaRembolso { get; set; }
        public decimal? CantidadADevolver { get; set; }
        public required string MotivoRembolso { get; set; }
        public required List<PayPalPaymentItem> Productos { get; set; }
    }
}
