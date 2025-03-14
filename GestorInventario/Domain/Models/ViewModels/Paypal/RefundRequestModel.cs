namespace GestorInventario.Domain.Models.ViewModels.Paypal
{
    public class RefundRequestModel
    {
        public int PedidoId { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; } 
    }
}
