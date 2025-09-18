using GestorInventario.enums;
using GestorInventario.Validations;
using System.ComponentModel.DataAnnotations;

namespace GestorInventario.ViewModels.product
{
    public class ProductosViewModel
    {
        public int Id { get; set; }
        public required string NombreProducto { get; set; }
        public required string Descripcion { get; set; }
        public int Cantidad { get; set; }
        public string? Imagen { get; set; }
        [PesoArchivoValidacion(5)]
        [TipoArchivoValidacion(GrupoTipoArchivo.Imagen)]
        public IFormFile? Imagen1 { get; set; }
        public DateTime? FechaCreacion { get; set; }
        public decimal Precio { get; set; }
        [Display(Name ="Proveedor")]
        public int? IdProveedor { get; set; }

    }
}
