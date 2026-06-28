using GestorInventario.Domain.Models;

using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels.Pedidos
{
    public class PedidoViewModel
    {
        public required List<Pedido> Pedidos { get; set; }
        public required List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public required string Buscar { get; set; }    
    }
}
