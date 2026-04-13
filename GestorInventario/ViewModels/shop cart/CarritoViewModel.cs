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
        public decimal Impuestos { get; set; }   // ← Nuevo
        public decimal Shipping { get; set; } = 0.00m; // Costo de envío fijo
        public decimal Total { get; set; }
    }
}
