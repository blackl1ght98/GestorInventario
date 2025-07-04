using GestorInventario.PaginacionLogica;

namespace GestorInventario.Domain.Models.ViewModels.order
{
    public class HistorialPedidoViewModel
    {
        public List<HistorialPedido> HistorialPedidos { get; set; }
        public List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public string Buscar { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }
}
