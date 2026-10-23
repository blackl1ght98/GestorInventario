namespace GestorInventario.Application.DTOS.Paypal
{
    public record ProductoPaypalDto
    {
        public required string Id { get; init; }
        public required string Nombre { get; init; }
        public required string Descripcion { get; init; }
    }
}
