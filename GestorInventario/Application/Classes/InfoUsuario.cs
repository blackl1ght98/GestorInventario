using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.Classes
{
    public class InfoUsuario
    {
        [Required] 
        public string nombreCompletoUsuario { get; set; }
        [Required]
        public string telefono { get; set; }
        [Required]
        public string codigoPostal { get; set; }
        [Required]
        public string ciudad { get; set; }
        public string direccion { get; set; }
        public string line1 { get; set; }
        public string line2 { get; set; }
    }
   
        
       
      
     
    
}