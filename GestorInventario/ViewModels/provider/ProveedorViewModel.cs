using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.provider
{
    public class ProveedorViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage ="El nombre del proveedor es requerido")]
        [Display(Name ="Nombre del Proveedor")]
        public  string? NombreProveedor { get; set; }
        [Required(ErrorMessage ="El contacto del proveedor es requerido")]
        public  string? Contacto { get; set; }
        [Required(ErrorMessage ="La direccion del proveedor es requerida")]
        public  string? Direccion { get; set; }
        [Required(ErrorMessage ="Elija un proveedor")]
        public int? IdUsuario { get; set; }
        public IEnumerable<SelectListItem> Usuarios { get; set; } = Enumerable.Empty<SelectListItem>();
    }
   
}
