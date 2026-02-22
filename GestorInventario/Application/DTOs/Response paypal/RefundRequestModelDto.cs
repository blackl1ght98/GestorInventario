namespace GestorInventario.Application.DTOs.Response_paypal
{
    public class RefundRequestModelDto
    {
        public required int PedidoId { get; set; }
        public decimal Amount { get; set; }
        public required string Currency { get; set; }
        public  string Motivo { get; set; }
    }
}
