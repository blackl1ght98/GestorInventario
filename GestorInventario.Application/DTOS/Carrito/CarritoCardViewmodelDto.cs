namespace GestorInventario.Application.DTOS.Carrito;

public class CarritoCardViewmodelDto
{
    public string Imagen { get; set; }
    public string NombreProducto { get; set; }
    public decimal Precio { get; set; }
    public string Descripcion { get; set; }
    public string NombreProveedor { get; set; }
    public int IdDetallePedido { get; set; }
    public int Cantidad { get; set; }
    public decimal  Total { get; set; }
}