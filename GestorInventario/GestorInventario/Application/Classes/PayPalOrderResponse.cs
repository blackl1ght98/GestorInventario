namespace GestorInventario.Application.Classes
{
    public class PayPalOrderResponse
    {
        public string id { get; set; }
        public string status { get; set; }
        public List<PayPalLink> links { get; set; }
    }

    public class PayPalLink
    {
        public string href { get; set; }
        public string rel { get; set; }
        public string method { get; set; }
    }

}
