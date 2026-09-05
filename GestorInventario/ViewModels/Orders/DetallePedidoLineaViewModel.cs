namespace GestorInventario.ViewModels.Orders
{
    public class DetallePedidoLineaViewModel
    {
        public int DetalleId { get; set; }
        public string NombreProducto { get; set; }
        public string? Descripcion { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal SubtotalSinIva { get; set; }
        public decimal Iva { get; set; }
        public decimal TotalConIva { get; set; }
        public bool Rembolsado { get; set; }
    }
}
