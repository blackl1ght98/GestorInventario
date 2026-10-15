namespace GestorInventario.Application.DTOS.Auth
{
    public class HashResult
    {
        public required string Hash { get; set; }
        public required byte[] Salt { get; set; }
    }
}
