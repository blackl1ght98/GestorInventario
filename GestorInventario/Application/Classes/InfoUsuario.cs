using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.Classes
{
    public class InfoUsuario
    {
        [Required] 
        public required string NombreCompletoUsuario { get; set; }
        [Required]
        public required string Telefono { get; set; }
        [Required]
        public required string CodigoPostal { get; set; }
        [Required]
        public required string Ciudad { get; set; }       
        public required string Line1 { get; set; }
        public required string Line2 { get; set; }
    }                           
}