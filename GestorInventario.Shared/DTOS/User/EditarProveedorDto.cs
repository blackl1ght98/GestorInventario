using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Shared.DTOS.User
{
    public class EditarProveedorDto
    {
        public int Id { get; set; }
       
        public string? NombreProveedor { get; set; }
        
        public string? Contacto { get; set; }
       
        public string? Direccion { get; set; }
  
        public int IdUsuario { get; set; }
      
    }
}
