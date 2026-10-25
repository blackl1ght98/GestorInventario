namespace GestorInventario.Application.DTOS.Auth
{
    public record HashResult
    {
        public required string Hash { get; init; }
        public required byte[] Salt { get; init; }
    }
}
