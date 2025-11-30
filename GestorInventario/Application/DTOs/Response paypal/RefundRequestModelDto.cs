namespace GestorInventario.Application.DTOs.Response_paypal
{
    public class RefundRequestModelDto
    {
        public int PedidoId { get; set; }
        public decimal Amount { get; set; }
        public required string Currency { get; set; }
        public required string Motivo { get; set; }
    }
}
