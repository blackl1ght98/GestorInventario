namespace GestorInventario.ViewModels.Paypal
{
    public class RefundRequestModel
    {
        public int PedidoId { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; } 
    }
    public class RefundForm
    {
        public string NumeroPedido { get; set; }
        public string NombreCliente { get; set; }
        public string EmailCliente { get; set; }
        public DateTime FechaRembolso { get; set; }
       
        public string MotivoRembolso { get; set; }
    }
}
