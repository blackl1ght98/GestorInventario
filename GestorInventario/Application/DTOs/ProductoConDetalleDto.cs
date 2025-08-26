using GestorInventario.Domain.Models;

namespace GestorInventario.Application.DTOs
{
    public class ProductoConDetalleDto
    {
        public Producto Producto { get; set; }
        public int? Cantidad { get; set; } // Cambiado a int?
        public decimal PrecioUnitario { get; set; }
        public decimal SubTotal => Cantidad.GetValueOrDefault(0) * PrecioUnitario; // Ajustar SubTotal
    }
}
