namespace GestorInventario.Domain.Models.ViewModels.paypal
{
    public class PaypalProductResponse
    {
        public List<PaypalProduct> Products { get; set; } = new List<PaypalProduct>();
    }

    public class PaypalProduct
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
