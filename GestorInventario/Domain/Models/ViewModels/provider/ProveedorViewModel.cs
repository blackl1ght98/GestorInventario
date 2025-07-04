using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestorInventario.Domain.Models.ViewModels.provider
{
    public class ProveedorViewModel
    {
        public int Id { get; set; }
        public string NombreProveedor { get; set; }
        public string Contacto { get; set; }
        public string Direccion { get; set; }
        public int? IdUsuario { get; set; } 
        public IEnumerable<SelectListItem> Usuarios { get; set; } 
    }
   
}
