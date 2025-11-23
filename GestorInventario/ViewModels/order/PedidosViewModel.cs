using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.order
{
    public class PedidosViewModel
    {

        public List<ProductoPedidoViewModel> Productos { get; set; } = new();

        public string? NumeroPedido { get; set; }
        public DateTime FechaPedido { get; set; }
        public string? EstadoPedido { get; set; }
        public int? IdUsuario { get; set; }

    }
    public class ProductoPedidoViewModel
    {
        public int ProductoId { get; set; }
        public string? Nombre { get; set; } // opcional, para mostrarlo en la vista
        public bool Seleccionado { get; set; }
        public int Cantidad { get; set; }
    }
}
