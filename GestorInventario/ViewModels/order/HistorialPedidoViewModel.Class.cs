using GestorInventario.Domain.Models;
using GestorInventario.PaginacionLogica;

namespace GestorInventario.ViewModels.order
{
    public class HistorialPedidoViewModel
    {
        public List<HistorialPedido> HistorialPedidos { get; set; }
        public List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public string Buscar { get; set; }
    
    }
}
