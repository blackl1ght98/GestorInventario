using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.DTOs
{
    public class InfoUsuarioDto
    {
    
        public required string NombreCompletoUsuario { get; set; }
        public required string CodigoPostal { get; set; }
        public string Telefono { get; set; }
        public required string Ciudad { get; set; }       
        public required string Line1 { get; set; }
        public required string Line2 { get; set; }
    }                           
}