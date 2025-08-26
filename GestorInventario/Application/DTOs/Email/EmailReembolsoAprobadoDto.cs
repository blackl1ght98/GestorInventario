using GestorInventario.Domain.Models;

namespace GestorInventario.Application.DTOs.Email
{
    public class EmailReembolsoAprobadoDto
    {
        public string NumeroPedido { get; set; }
        public string NombreCliente { get; set; }
        public string EmailCliente { get; set; }
        public DateTime FechaRembolso { get; set; }
        public decimal CantidadADevolver { get; set; } // Monto reembolsado
        public string MotivoRembolso { get; set; }
        public List<PayPalPaymentItem> Productos { get; set; } // Lista de productos
    }
}
