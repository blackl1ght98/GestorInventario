using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.user
{
    public class UsuarioEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        public required string Email { get; set; }
     
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [Display(Name ="Nombre Completo")]
        public required string NombreCompleto { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es requerida")]
        public DateTime? FechaNacimiento { get; set; }

        [Required(ErrorMessage = "El telefono es requerido")]
        public  string? Telefono { get; set; }

        [Required(ErrorMessage = "La dirección es requerida")]
        public required string Direccion { get; set; }

        [Required(ErrorMessage = "La ciudad es requerida")]
        public required string Ciudad { get; set; }
        [Required(ErrorMessage = "El codigo postal es requerido")]
        [Display(Name ="Codigo Postal")]
        public required string  CodigoPostal { get; set; }
        public bool EsEdicionPropia { get; set; }

    }
}
