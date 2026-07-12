namespace GestorInventario.Shared.DTOS.Products
{
    public class ProductoDto
    {

        public required string NombreProducto { get; set; }
        public required string Descripcion { get; set; }
        public int Cantidad { get; set; }
        public byte[]? ArchivoImagenBytes { get; set; }
        public string? ArchivoImagenNombre { get; set; }
        public string? ArchivoImagenContentType { get; set; }
        public decimal Precio { get; set; }
        public int IdProveedor { get; set; }

    }
}
