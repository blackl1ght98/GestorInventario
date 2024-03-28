namespace GestorInventario.Application.DTOs
{
    public class DTOEmail
    {
        public string ToEmail { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public bool? IsHtml { get; set; }
        public string? RecoveryLink { get; set; }
        public string? TemporaryPassword { get; set; }
    }
}
