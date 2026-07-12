using GestorInventario.enums.Archivos;
using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Shared.DTOS.User
{
    public class ProductoDto
    {

        public required string NombreProducto { get; set; }
        public required string Descripcion { get; set; }
        public int Cantidad { get; set; }
        public string? Imagen { get; set; }

       
        public byte[]? ArchivoImagenBytes { get; set; }
        public string? ArchivoImagenNombre { get; set; }
        public string? ArchivoImagenContentType { get; set; }

        public decimal Precio { get; set; }
        public int IdProveedor { get; set; }

    }
}
