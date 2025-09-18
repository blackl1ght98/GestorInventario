namespace GestorInventario.Application.DTOs.Email
{
    public class EmailDto
    {
        public  string? ToEmail { get; set; }      
        public  string? RecoveryLink { get; set; }
        public  string? TemporaryPassword { get; set; }
        public  string? NombreProducto { get; set; }
        public int Cantidad { get; set; }
       
    }
}
