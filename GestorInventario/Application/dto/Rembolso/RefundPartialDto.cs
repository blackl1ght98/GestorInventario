namespace GestorInventario.Application.DTOs.Rembolso
{
    public class RefundPartialDto
    {
        public required int DetalleId { get; set; }
       
        public required string Currency { get; set; }
        public  string Motivo { get; set; }
    }
}
