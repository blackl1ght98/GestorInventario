using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Models.ViewModels
{
    public class UserViewModel
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        public int IdRol { get; set; } = 1;
        [Required]
        public string NombreCompleto { get; set; }
        [Required]
        public DateTime? FechaNacimiento { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }
        public string codigoPostal { get; set; }
        public string ciudad { get; set; }
    }
}
