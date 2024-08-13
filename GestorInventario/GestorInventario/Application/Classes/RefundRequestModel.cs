namespace GestorInventario.Application.Classes
{
    public class RefundRequestModel
    {
        public int PedidoId { get; set; }
        public decimal RefundAmount { get; set; }
        public string Currency { get; set; } = "EUR";
    }
}
