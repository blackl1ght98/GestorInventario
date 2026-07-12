using GestorInventario.Domain.Models;

using GestorInventario.Shared.Utilities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestorInventario.ViewModels.Products
{
    public class ProductsIndexViewModel
    {
        public required List<Producto> Productos { get; set; }
        public required List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public required string Buscar { get; set; }
        public required string OrdenarPorPrecio { get; set; }
        public int? IdProveedor { get; set; }
        public required  SelectList Proveedores { get; set; }
    }
}
