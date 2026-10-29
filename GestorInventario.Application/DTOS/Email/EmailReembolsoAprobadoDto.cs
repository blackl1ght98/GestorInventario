using GestorInventario.Application.DTOS.Rembolso;

namespace GestorInventario.Application.DTOS.Email
{
    public record EmailReembolsoAprobadoDto
    {
        public required string NumeroPedido { get; init; }
        public required string NombreCliente { get; init; }
        public required string EmailCliente { get; init; }
        public DateTime? FechaRembolso { get; init; }
        public decimal CantidadADevolver { get; init; } 
        public required string MotivoRembolso { get; init; }
        public required List<PaypalPaymentItemDto> Productos { get; init; } // Lista de productos
        public bool EsReembolsoTotal { get; init; }
    }
}
