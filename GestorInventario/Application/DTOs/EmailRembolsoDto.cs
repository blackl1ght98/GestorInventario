using GestorInventario.Domain.Models;

namespace GestorInventario.Application.DTOs
{
    public class EmailRembolsoDto
    {
        public string NumeroPedido { get; set; }
        public string NombreCliente { get; set; }
        public string EmailCliente { get; set; }
        public DateTime? FechaRembolso { get; set; }
        public decimal? CantidadADevolver { get; set; }
        public string MotivoRembolso { get; set; }
        public List<PayPalPaymentItem> Productos { get; set; }
    }
}
