namespace GestorInventario.ViewModels.Paypal
{
 
    public class RefundFormViewModel
    {
        public required string NumeroPedido { get; set; }
        public required string NombreCliente { get; set; }
        public required string EmailCliente { get; set; }
        public DateTime FechaRembolso { get; set; }
       
        public required string MotivoRembolso { get; set; }
    }
}
