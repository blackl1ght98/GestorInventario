using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestorInventario.Domain.Models.ViewModels
{
    public class CarritoViewModel
    {
        public required List<DetallePedido> Productos { get; set; }
        public required SelectList Monedas { get; set; }
        public required List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; } = 2.99m; // Costo de envío fijo
        public decimal Total { get; set; }
    }
}
