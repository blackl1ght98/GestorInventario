using GestorInventario.Domain.Models;

namespace GestorInventario.ViewModels
{
    public class EmailRembolsoViewmodel
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
