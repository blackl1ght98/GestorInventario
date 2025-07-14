using GestorInventario.Application.DTOs;

namespace GestorInventario.ViewModels.Paypal
{
    public class PlanesViewModel
    {
        public string id { get; set; }
        public string productId { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string status { get; set; }
        public string usage_type { get; set; }
        public DateTime createTime { get; set; }
        public List<BillingCycle> billing_cycles { get; set; }
        public Taxes Taxes { get; set; }
    }
}