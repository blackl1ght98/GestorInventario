using GestorInventario.Domain.Models;

namespace GestorInventario.ViewModels.Email
{
    public class RefundApprovedViewmodel
    {
        public required string NumeroPedido { get; set; }
        public required string NombreCliente { get; set; }
        public required string EmailCliente { get; set; }
        public DateTime? FechaRembolso { get; set; }
        public decimal CantidadADevolver { get; set; } // Monto reembolsado
        public required string MotivoRembolso { get; set; }
        public required List<PayPalPaymentItem> Productos { get; set; } // Lista de productos
    }
}
