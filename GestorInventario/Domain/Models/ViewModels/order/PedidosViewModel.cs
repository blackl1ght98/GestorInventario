using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Domain.Models.ViewModels.order
{
    public class PedidosViewModel
    {
        public int Id { get; set; }
        [Display(Name = "Productos")]
        public List<int> IdsProducto { get; set; } // Lista de IDs de productos, esta variable almacena el total de productos que hay
        public List<int> Cantidades { get; set; } // Lista de cantidades, esto almacena la cantidad de cada producto
        public List<bool> ProductosSeleccionados { get; set; }//estado del checkbook, al crear el pedido detecta cuales pedidos se han seleccionado
        public string NumeroPedido { get; set; }
        public DateTime FechaPedido { get; set; }
        public string EstadoPedido { get; set; }
        [Display(Name = "Clientes")]
        public int? IdUsuario { get; set; }
       
    }

}
