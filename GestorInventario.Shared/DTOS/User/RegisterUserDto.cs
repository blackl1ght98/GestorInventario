namespace GestorInventario.Shared.DTOS.User
{
    public class RegisterUserDto
    {
       
        public required string Email { get; set; }

        public required string Password { get; set; }
      
        public int IdRol { get; set; } = 2;
        
        public required string NombreCompleto { get; set; }
        
        public DateTime? FechaNacimiento { get; set; }
       
        public required string Telefono { get; set; }
       
        public required string Direccion { get; set; }
       
        public required string CodigoPostal { get; set; }
       
        public required string Ciudad { get; set; }
    }
}
