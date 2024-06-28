namespace GestorInventario.Application.DTOs
{
    public class DTOLoginResponse
    {
        public int Id { get; set; }
        public string? Token { get; set; }
        public string? Rol { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public DateTime? FechaRegistro { get; set; }
    }
}
