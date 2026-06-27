using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.DTOS.User
{
    public class CrearProveedorDto
    {
   
       
        public string? NombreProveedor { get; set; }
        
        public string? Contacto { get; set; }
      
        public string? Direccion { get; set; }
     
        public int IdUsuario { get; set; }
     
    }
}
