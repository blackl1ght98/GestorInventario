namespace GestorInventario.ViewModels.Products
{
    public class DeleteProductoViewmodel
    {
        public int Id { get; set; }
        public string NombreProducto { get; set; }
        public string Descripcion { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public string NombreProvedor { get; set; }
    }
}
