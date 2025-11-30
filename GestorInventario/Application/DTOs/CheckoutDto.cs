using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.DTOs
{
    public class CheckoutDto
    {       
        public required List<ItemModelDto> Items { get; set; }
        public required decimal TotalAmount { get; set; }      
        public required string ReturnUrl { get; set; }
        public required string CancelUrl { get; set; }
        public required string Currency { get; set; }
        public required string NombreCompleto { get; set; }
        public required string Telefono { get; set; }
        public required string CodigoPostal { get; set; }
        public required string  Ciudad { get; set; }
        public required string Line1 { get; set; }
        public required string Line2 { get; set; }
    }
}
