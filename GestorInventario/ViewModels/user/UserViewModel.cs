using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.user
{
    public class UserViewModel
    {
        [Required]
        public required string Email { get; set; }
        [Required]
        public required string Password { get; set; }
        public int IdRol { get; set; } = 1;
        [Required]
        public required string NombreCompleto { get; set; }
        [Required]
        public DateTime? FechaNacimiento { get; set; }
        public required string Telefono { get; set; }
        public required string Direccion { get; set; }
        public required string CodigoPostal { get; set; }
        public required string Ciudad { get; set; }
    }
}
