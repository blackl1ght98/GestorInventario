using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.user
{
    public class UsuarioEditViewModel
    {
        public int Id { get; set; }

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
        public int IdRol { get; set; }
        public string Ciudad { get; set; }
        public string  codigoPostal { get; set; }
        public bool EsEdicionPropia { get; set; }

    }
}
