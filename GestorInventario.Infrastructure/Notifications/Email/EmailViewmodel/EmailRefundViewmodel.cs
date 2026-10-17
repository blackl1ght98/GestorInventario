using GestorInventario.Application.DTOS.Rembolso;

namespace GestorInventario.Infrastructure.Notifications.Email.EmailViewmodel
{
    public class EmailRefundViewmodel
    {
        public required string NumeroPedido { get; set; }
        public required string NombreCliente { get; set; }
        public required string EmailCliente { get; set; }
        public DateTime? FechaRembolso { get; set; }
        public decimal? CantidadADevolver { get; set; }
        public required string MotivoRembolso { get; set; }
        public required List<PaypalPaymentItemDto> Productos { get; set; }
    }
}
