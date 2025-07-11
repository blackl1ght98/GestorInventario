using GestorInventario.Domain.Models;
using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestorInventario.ViewModels.product
{
    public class ProductsViewModel
    {
        public List<Producto> Productos { get; set; }
        public List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public string Buscar { get; set; }
        public string OrdenarPorPrecio { get; set; }
        public int? IdProveedor { get; set; }
        public SelectList Proveedores { get; set; }
    }
}
