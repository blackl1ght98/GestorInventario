using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.user
{
    public class UserViewModel
    {
        [Required(ErrorMessage ="El email es requerido")]
        public required string Email { get; set; }
        [Required(ErrorMessage ="La contraseña es requerida")]
        public required string Password { get; set; }
        [Required(ErrorMessage ="El rol es requerido")]
        public int IdRol { get; set; } = 1;
        [Required(ErrorMessage ="El nombre completo es requerido")]
        public required string NombreCompleto { get; set; }
        [Required(ErrorMessage ="La fecha de nacimiento es requerida")]
        public DateTime? FechaNacimiento { get; set; }
        [Required(ErrorMessage ="El telefono es requerido")]
        public required string Telefono { get; set; }
        [Required(ErrorMessage ="La dirección es requerida")]
        public required string Direccion { get; set; }
        [Required(ErrorMessage ="El codigo postal es requerido")]
        public required string CodigoPostal { get; set; }
        [Required(ErrorMessage ="La ciudad es requerida")]
        public required string Ciudad { get; set; }
    }
}
