namespace GestorInventario.Application.Classes
{
    public class ResultadoHash
    {
        public required string Hash { get; set; }
        public required byte[] Salt { get; set; }
    }
}
