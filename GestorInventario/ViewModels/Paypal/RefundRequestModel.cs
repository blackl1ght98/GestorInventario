namespace GestorInventario.ViewModels.Paypal
{
    public class RefundRequestModel
    {
        public int PedidoId { get; set; }
        public decimal Amount { get; set; }
        public required string Currency { get; set; }
        public required string Motivo { get; set; }
    }
    public class RefundForm
    {
        public required string NumeroPedido { get; set; }
        public required string NombreCliente { get; set; }
        public required string EmailCliente { get; set; }
        public DateTime FechaRembolso { get; set; }
       
        public required string MotivoRembolso { get; set; }
    }
}
