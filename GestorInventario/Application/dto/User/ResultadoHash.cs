namespace GestorInventario.Application.DTOs.User
{
    public class ResultadoHash
    {
        public required string Hash { get; set; }
        public required byte[] Salt { get; set; }
    }
}
