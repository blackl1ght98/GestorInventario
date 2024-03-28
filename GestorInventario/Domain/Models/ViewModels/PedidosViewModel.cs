using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Domain.Models.ViewModels
{
    public class PedidosViewModel
    {
        public int Id { get; set; }
        [Display(Name = "Productos")]
        public List<int> IdsProducto { get; set; } // Lista de IDs de productos
        public int Cantidad { get; set; }
        public List<bool> ProductosSeleccionados { get; set; }//estado del checkbook
        public string NumeroPedido { get; set; }
        public DateTime FechaPedido { get; set; }
        public string EstadoPedido { get; set; }
        [Display(Name = "Clientes")]
        public int? IdUsuario { get; set; }
    }

}
