namespace GestorInventario.Shared.DTOS.Auth
{
    public class ResultadoHash
    {
        public required string Hash { get; set; }
        public required byte[] Salt { get; set; }
    }
}
