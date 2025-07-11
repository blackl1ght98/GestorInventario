using GestorInventario.Domain.Models;
using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestorInventario.ViewModels.product
{
    public class HistorialProductoViewModel
    {
        public List<HistorialProducto> Historial { get; set; }
        public List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
      
    }
}
