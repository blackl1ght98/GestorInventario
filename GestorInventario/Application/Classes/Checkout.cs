using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.Classes
{
    public class Checkout
    {       
        public required List<ItemModel> Items { get; set; }
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
