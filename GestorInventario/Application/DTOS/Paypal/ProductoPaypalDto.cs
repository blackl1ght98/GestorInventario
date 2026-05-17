namespace GestorInventario.Application.DTOS.Paypal
{
    public class ProductoPaypalDto
    {
        public required string Id { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
    }
}
