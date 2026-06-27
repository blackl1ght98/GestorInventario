using GestorInventario.enums.Archivos;
using GestorInventario.Validations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.DTOS.User
{
    public class ProductoDto
    {

        public required string NombreProducto { get; set; }
        public required string Descripcion { get; set; }
        public int Cantidad { get; set; }
        public string? Imagen { get; set; }

        // Antes: public IFormFile? ArchivoImagen { get; set; }
        // Ahora: solo datos
        public byte[]? ArchivoImagenBytes { get; set; }
        public string? ArchivoImagenNombre { get; set; }
        public string? ArchivoImagenContentType { get; set; }

        public decimal Precio { get; set; }
        public int IdProveedor { get; set; }

    }
}
