using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestorInventario.Domain.Models.ViewModels
{
    public class CarritoViewModel
    {
        public List<ItemsDelCarrito> Productos { get; set; }
        public SelectList Monedas { get; set; }
        public List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; } = 2.99m; // Costo de envío fijo
        public decimal Total { get; set; }
    }
}
