using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.Classes
{
    public class Checkout
    {
       
        public List<ItemModel> items { get; set; }
        public decimal totalAmount { get; set; }
       
        public string returnUrl { get; set; }
        public string cancelUrl { get; set; }
        public string currency { get; set; }
        public string nombreCompleto { get; set; }
        public string telefono { get; set; }
        public string codigoPostal { get; set; }
        public string  ciudad { get; set; }
        public string line1 { get; set; }
        public string line2 { get; set; }
    }
}
