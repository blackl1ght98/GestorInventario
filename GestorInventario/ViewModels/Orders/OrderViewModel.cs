using GestorInventario.Domain.Models;

using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels.Orders
{
    public class OrderViewModel
    {
        public required List<Pedido> Pedidos { get; set; }
        public required List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public required string Buscar { get; set; }    
    }
}
