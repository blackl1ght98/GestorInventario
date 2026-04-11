using GestorInventario.enums;
using GestorInventario.Validations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.product
{
    public class ProductosViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage ="El nombre del producto es requerido")]
        [Display(Name ="Nombre del Producto")]
        public required string NombreProducto { get; set; }
        [Required(ErrorMessage ="La descripcion del producto es requerida")]
        public required string Descripcion { get; set; }
        [Required(ErrorMessage ="La cantidad del producto es requerida")]
        public int Cantidad { get; set; }
        public string? Imagen { get; set; }
        [PesoArchivoValidacion(5)]
        [TipoArchivoValidacion(GrupoTipoArchivo.Imagen)]
        public IFormFile? ArchivoImagen { get; set; }
        [Required(ErrorMessage ="El precio del producto es requerido")]
        public decimal Precio { get; set; }
        [Display(Name ="Seleccione un proveedor")]
        public int? IdProveedor { get; set; }
        public IEnumerable<SelectListItem> Proveedores { get; set; } = Enumerable.Empty<SelectListItem>();

    }
}
