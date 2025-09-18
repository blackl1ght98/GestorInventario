using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.user
{
    public class UsuarioEditViewModel
    {
        public int Id { get; set; }

        [Required]
        public required string Email { get; set; }
     
        [Required]
        public required string NombreCompleto { get; set; }

        [Required]
        public DateTime? FechaNacimiento { get; set; }

        [Required]
        public  string? Telefono { get; set; }

        [Required]
        public required string Direccion { get; set; } 
        public int IdRol { get; set; }
        public required string Ciudad { get; set; }
        public required string  CodigoPostal { get; set; }
        public bool EsEdicionPropia { get; set; }

    }
}
