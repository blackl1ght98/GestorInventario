using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.order
{
    public class EditPedidoViewModel
    {
        public int id { get; set; }
        public DateTime fechaPedido { get; set; }

        public string estadoPedido { get; set; }
        public List<int>? IdsProducto { get; set; } // Lista de IDs de productos, esta variable almacena el total de productos que hay
        public List<int>? Cantidades { get; set; } // Lista de cantidades, esto almacena la cantidad de cada producto
        public List<bool>? ProductosSeleccionados { get; set; }//estado del checkbook, al crear el pedido detecta cuales pedidos se han seleccionado
        [Display(Name = "Clientes")]
        public int? IdUsuario { get; set; }
    }
}
