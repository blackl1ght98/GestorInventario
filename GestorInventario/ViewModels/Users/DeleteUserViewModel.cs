namespace GestorInventario.ViewModels.Users
{
    public class DeleteUserViewModel
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; }
        public string Email { get; set; }
        public string? Direccion { get; set; }
        public string? Ciudad { get; set; }
        public int? CodigoPostal { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public DateTime? FechaRegistro { get; set; }
    }
}
