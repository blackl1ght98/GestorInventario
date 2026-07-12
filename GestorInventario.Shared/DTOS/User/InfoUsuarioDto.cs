

namespace GestorInventario.Shared.DTOS.User
{
    public class InfoUsuarioDto
    {
    
        public required string NombreCompletoUsuario { get; set; }
        public  required int CodigoPostal { get; set; }
        public  required string Telefono { get; set; }
        public required string Ciudad { get; set; }       
        public required string Line1 { get; set; }
        public required string Line2 { get; set; }
    }                           
}