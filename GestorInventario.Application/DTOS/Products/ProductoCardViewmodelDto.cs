namespace GestorInventario.Application.DTOS.Products;

public class ProductoCardViewmodelDto
{
    public string Imagen { get; set; }
    public string NombreProducto { get; set; }
    public string NombreProveedor { get; set; }
    public string Descripcion { get; set; }
    public int Cantidad { get; set; }
    public decimal Precio { get; set; }
    public int Id { get; set; }
}