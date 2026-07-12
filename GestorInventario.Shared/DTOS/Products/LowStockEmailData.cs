namespace GestorInventario.Shared.DTOS.Products
{
    public class LowStockEmailData
    {
        public string NombreProducto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }
}
