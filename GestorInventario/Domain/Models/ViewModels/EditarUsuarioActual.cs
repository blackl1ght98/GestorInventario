using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Domain.Models.ViewModels
{
    public class EditarUsuarioActual
    {
        

        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public string NombreCompleto { get; set; } = null!;

        [Required]
        public DateTime? FechaNacimiento { get; set; }

        [Required]
        public string? Telefono { get; set; }

        [Required]
        public string Direccion { get; set; } = null!;
        public string ciudad { get; set; }
        public string codigoPostal { get; set; }
    }
}
