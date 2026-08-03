namespace GestorInventario.Shared.DTOS.Paypal.Projections
{
    public record ProductoProjection
    {
        public required string Id { get; init; }
        public required string Nombre { get; init; }
        public required string Descripcion { get; init; }
    }
}
