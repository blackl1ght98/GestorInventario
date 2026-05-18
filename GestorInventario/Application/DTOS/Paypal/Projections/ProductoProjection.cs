namespace GestorInventario.Application.DTOS.Paypal.Projections
{
    public class ProductoProjection
    {
        public required string Id { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
    }
}
