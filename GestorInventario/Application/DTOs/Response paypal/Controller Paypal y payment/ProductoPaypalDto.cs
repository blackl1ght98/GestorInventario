namespace GestorInventario.Application.DTOs.Response_paypal
{
    public class ProductoPaypalDto
    {
        public required string Id { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
    }
}
