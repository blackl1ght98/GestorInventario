using GestorInventario.Application.DTOS.Rembolso;

namespace GestorInventario.Application.DTOS.Email
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
