namespace GestorInventario.Domain.Models.ViewModels
{
    public class EditPedidoViewModel
    {
        public int id { get; set; }
        public DateTime fechaPedido { get; set; }
        public string estadoPedido { get; set; }
    }
}
