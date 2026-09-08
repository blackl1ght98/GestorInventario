using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Rembolso;

namespace GestorInventario.Shared.DTOS.Email
{
    public class EmailReembolsoAprobadoDto
    {
        public required string NumeroPedido { get; set; }
        public required string NombreCliente { get; set; }
        public required string EmailCliente { get; set; }
        public DateTime? FechaRembolso { get; set; }
        public decimal CantidadADevolver { get; set; } 
        public required string MotivoRembolso { get; set; }
        public required List<PaypalPaymentItemDto> Productos { get; set; } // Lista de productos
        public bool EsReembolsoTotal { get; set; }
    }
}
