using GestorInventario.enums.Archivos;
using GestorInventario.Validations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.DTOS.User
{
    public class EditarProductoDto
    {
        public int Id { get; set; }
        public required string NombreProducto { get; set; }
        public required string Descripcion { get; set; }
        public int Cantidad { get; set; }
        public string? Imagen { get; set; }

        // Antes: IFormFile + DataAnnotations de archivos
        // Ahora: bytes + nombre + content-type (validaciones van en el ViewModel)
        public byte[]? ArchivoImagenBytes { get; set; }
        public string? ArchivoImagenNombre { get; set; }
        public string? ArchivoImagenContentType { get; set; }

        public decimal Precio { get; set; }
        public int IdProveedor { get; set; }
    }
}
